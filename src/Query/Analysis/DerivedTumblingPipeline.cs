using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Core;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivedTumblingPipeline
{
    private static readonly ModuleBuilder _module = AssemblyBuilder
        .DefineDynamicAssembly(new AssemblyName("KafkaKsqlLinq.DerivedTumbling"), AssemblyBuilderAccess.Run)
        .DefineDynamicModule("Main");
    public static async Task RunAsync(
        TumblingQao qao,
        EntityModel baseModel,
        KsqlQueryModel queryModel,
        Func<string, Task> execute,
        Func<string, Type> resolveType,
        MappingRegistry mapping,
        ConcurrentDictionary<Type, EntityModel> registry,
        ILogger logger)
    {
        var baseAttr = baseModel.EntityType.GetCustomAttribute<KsqlTopicAttribute>();
        var baseName = (baseAttr?.Name ?? baseModel.TopicName ?? baseModel.EntityType.Name).ToLowerInvariant();
        var entities = PlanDerivedEntities(qao, baseModel, queryModel.WhenEmptyFiller != null);
        var models = AdaptModels(entities);
        foreach (var m in models)
        {
            var role = Enum.Parse<Role>((string)m.AdditionalSettings["role"]);
        var (ddl, dt, ns) = BuildDdlAndRegister(baseName, queryModel, m, role, resolveType);
        logger.LogInformation("KSQL DDL (derived {Entity}): {Sql}", m.TopicName, ddl);
        await execute(ddl);
            mapping.RegisterEntityModel(m, genericValue: true, overrideNamespace: ns);
            registry[dt] = m;
        }
    }

    public static IReadOnlyList<DerivedEntity> PlanDerivedEntities(TumblingQao qao, EntityModel model, bool whenEmpty)
        => DerivationPlanner.Plan(qao, model, whenEmpty);

    public static IReadOnlyList<EntityModel> AdaptModels(IReadOnlyList<DerivedEntity> entities)
        => EntityModelAdapter.Adapt(entities);

    private static (string ddl, Type entityType, string? ns) BuildDdlAndRegister(
        string baseName,
        KsqlQueryModel queryModel,
        EntityModel model,
        Role role,
        Func<string, Type> resolveType)
    {
        var qm = queryModel.Clone();
        if (role == Role.Final || role == Role.Prev1m)
        {
            if (qm.SelectProjection != null)
            {
                var (sel, grp) = BuildFinalProjection(model, qm.SelectProjection);
                qm.SelectProjection = sel;
                qm.GroupByExpression = grp;
            }
            qm.Windows.Clear();
        }
        var tf = (string)model.AdditionalSettings["timeframe"];
        Timeframe tfObj;
        if (tf.EndsWith("wk", StringComparison.OrdinalIgnoreCase))
            tfObj = new Timeframe(int.Parse(tf[..^2]), "wk");
        else if (tf.EndsWith("mo", StringComparison.OrdinalIgnoreCase))
            tfObj = new Timeframe(int.Parse(tf[..^2]), "mo");
        else
            tfObj = new Timeframe(int.Parse(tf[..^1]), tf[^1].ToString());
        var spec = RoleTraits.For(role, tfObj);
        var emit = spec.Emit != null ? $"EMIT {spec.Emit}" : null;
        var name = role switch
        {
            Role.Live => $"{baseName}_{tf}_live",
            Role.Final => $"{baseName}_{tf}_final",
            Role.Prev1m => $"{baseName}_prev_1m",
            Role.Hb => $"{baseName}_hb_{tf}",
            Role.Fill => $"{baseName}_{tf}_fill",
            _ => $"{baseName}_{tf}"
        };
        var inputOverride = baseName;
        if (model.AdditionalSettings.TryGetValue("input", out var inputObj))
            inputOverride = inputObj?.ToString() ?? baseName;
        string ddl;
        if (role == Role.Final || role == Role.Prev1m)
        {
            Func<Type, string> resolver = _ => inputOverride;
            ddl = KsqlCreateStatementBuilder.Build(name, qm, null, null, resolver);
            if (!string.IsNullOrWhiteSpace(emit))
                ddl = ddl.Replace("EMIT CHANGES", emit);
        }
        else
        {
            ddl = KsqlCreateWindowedStatementBuilder.Build(name, qm, tf, emit, inputOverride);
        }
        var dt = resolveType(name);
        model.EntityType = dt;
        model.TopicName = name;
        model.SetStreamTableType(qm.DetermineType());
        var ns = model.AdditionalSettings.TryGetValue("namespace", out var nsObj) ? nsObj?.ToString() : null;
        return (ddl, dt, ns);
    }

    private static (LambdaExpression select, LambdaExpression group) BuildFinalProjection(
        EntityModel model,
        LambdaExpression baseProjection)
    {
        var joinKeys = (string[])model.AdditionalSettings["basedOn/joinKeys"];
        var keyNames = (string[])model.AdditionalSettings["keys"];
        var keyTypes = (Type[])model.AdditionalSettings["keys/types"];
        var projNames = (string[])model.AdditionalSettings["projection"];
        var projTypes = (Type[])model.AdditionalSettings["projection/types"];

        var methods = new Dictionary<string, MethodInfo>();
        switch (baseProjection.Body)
        {
            case MemberInitExpression m:
                foreach (var b in m.Bindings.OfType<MemberAssignment>())
                    if (b.Expression is MethodCallExpression mc)
                        methods[b.Member.Name] = mc.Method.GetGenericMethodDefinition();
                break;
            case UnaryExpression { Operand: MemberInitExpression m }:
                foreach (var b in m.Bindings.OfType<MemberAssignment>())
                    if (b.Expression is MethodCallExpression mc)
                        methods[b.Member.Name] = mc.Method.GetGenericMethodDefinition();
                break;
            case NewExpression ne when ne.Members != null:
                for (int i = 0; i < ne.Members.Count; i++)
                    if (ne.Arguments[i] is MethodCallExpression mc)
                        methods[ne.Members[i].Name] = mc.Method.GetGenericMethodDefinition();
                break;
            case UnaryExpression { Operand: NewExpression ne } when ne.Members != null:
                for (int i = 0; i < ne.Members.Count; i++)
                    if (ne.Arguments[i] is MethodCallExpression mc)
                        methods[ne.Members[i].Name] = mc.Method.GetGenericMethodDefinition();
                break;
        }

        Type FindType(string name)
        {
            var idx = Array.FindIndex(keyNames, k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) return keyTypes[idx];
            idx = Array.FindIndex(projNames, k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) return projTypes[idx];
            return typeof(object);
        }

        var bucket = keyNames.FirstOrDefault(k => string.Equals(k, "BucketStart", StringComparison.OrdinalIgnoreCase)) ?? "BucketStart";

        var keyList = joinKeys.ToList();
        if (!keyList.Any(k => string.Equals(k, bucket, StringComparison.OrdinalIgnoreCase)))
            keyList.Add(bucket);

        var keyProps = keyList.Select(k => (Name: k, Type: FindType(k))).ToArray();
        var valueNames = projNames
            .Where(p => !keyList.Any(k => string.Equals(k, p, StringComparison.OrdinalIgnoreCase)))
            .Where(p => methods.ContainsKey(p))
            .ToArray();
        var valueProps = valueNames.Select(v => (Name: v, Type: FindType(v))).ToArray();
        var resultProps = keyProps.Concat(valueProps).ToArray();

        Type CreateType((string Name, Type Type)[] props)
        {
            var tb = _module.DefineType("T" + Guid.NewGuid().ToString("N"), TypeAttributes.Public);
            foreach (var p in props)
            {
                var field = tb.DefineField("_" + p.Name, p.Type, FieldAttributes.Private);
                var prop = tb.DefineProperty(p.Name, PropertyAttributes.None, p.Type, null);
                var get = tb.DefineMethod("get_" + p.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, p.Type, Type.EmptyTypes);
                var il = get.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Ret);
                var set = tb.DefineMethod("set_" + p.Name, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { p.Type });
                var il2 = set.GetILGenerator();
                il2.Emit(OpCodes.Ldarg_0);
                il2.Emit(OpCodes.Ldarg_1);
                il2.Emit(OpCodes.Stfld, field);
                il2.Emit(OpCodes.Ret);
                prop.SetGetMethod(get);
                prop.SetSetMethod(set);
            }
            return tb.CreateType()!;
        }

        var elementType = CreateType(resultProps);
        var keyType = CreateType(keyProps);
        var gType = typeof(IGrouping<,>).MakeGenericType(keyType, elementType);
        var g = Expression.Parameter(gType, "g");
        var keyExpr = Expression.Property(g, "Key");
        var bindings = new List<MemberBinding>();

        foreach (var kp in keyProps)
        {
            var prop = elementType.GetProperty(kp.Name)!;
            var val = Expression.Property(keyExpr, kp.Name);
            bindings.Add(Expression.Bind(prop, val));
        }

        var x = Expression.Parameter(elementType, "x");

        Expression Agg(MethodInfo method, string col, Type retType)
        {
            var selectorBody = Expression.Property(x, col);
            var selector = Expression.Lambda(selectorBody, x);
            MethodInfo mi;
            if (method.DeclaringType == typeof(OffsetAggregateExtensions))
                mi = method.MakeGenericMethod(elementType, keyType, retType);
            else if (method.GetGenericArguments().Length == 2)
                mi = method.MakeGenericMethod(elementType, retType);
            else
                mi = method.MakeGenericMethod(elementType);
            return Expression.Call(mi, g, selector);
        }

        foreach (var vp in valueProps)
        {
            var method = methods[vp.Name];
            bindings.Add(Expression.Bind(elementType.GetProperty(vp.Name)!, Agg(method, vp.Name, vp.Type)));
        }

        var selectBody = Expression.MemberInit(Expression.New(elementType), bindings);
        var select = Expression.Lambda(selectBody, g);

        var r = Expression.Parameter(elementType, "r");
        var keyBindings = keyProps.Select(kp => Expression.Bind(keyType.GetProperty(kp.Name)!, Expression.Property(r, kp.Name)));
        var group = Expression.Lambda(Expression.MemberInit(Expression.New(keyType), keyBindings), r);
        return (select, group);
    }
}
