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
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivedTumblingPipeline
{
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
            var tf = (string)m.AdditionalSettings["timeframe"];
            if (tf != "1s" && role != Role.Live)
                continue;
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
        var inputOverride = baseName;
        // For the 1s final stream, the input should be the original source stream (e.g., Rate/deduprates),
        // not the derived base name (bar). Try to infer from query model sources.
        if (role == Role.Final1sStream)
        {
            var src = queryModel.SourceTypes.FirstOrDefault();
            if (src != null)
            {
                var topicAttr = src.GetCustomAttribute<Kafka.Ksql.Linq.Core.Attributes.KsqlTopicAttribute>();
                inputOverride = (topicAttr?.Name ?? src.Name).ToLowerInvariant();
            }
        }
        else if (model.AdditionalSettings.TryGetValue("input", out var inputObj))
        {
            inputOverride = inputObj?.ToString() ?? baseName;
        }
        if (role == Role.Prev1m || role == Role.Final1sStream)
        {
            qm.Windows.Clear();
            qm.GroupByExpression = null;
            var inputType = resolveType(inputOverride);
            qm.SelectProjection = BuildInputProjection(inputType);
        }
        var tf = (string)model.AdditionalSettings["timeframe"];
        var spec = RoleTraits.For(role);
        var emit = spec.Emit != null ? $"EMIT {spec.Emit}" : null;
        var name = role switch
        {
            Role.Live => $"{baseName}_{tf}_live",
            Role.Final => $"{baseName}_{tf}_final",
            Role.Final1s => $"{baseName}_{tf}_final",
            Role.Final1sStream => $"{baseName}_{tf}_final_s",
            Role.Prev1m => $"{baseName}_prev_1m",
            Role.Hb => $"{baseName}_hb_{tf}",
            Role.Fill => $"{baseName}_{tf}_fill",
            _ => $"{baseName}_{tf}"
        };
        string ddl;
        if (role == Role.Prev1m || role == Role.Final1sStream)
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

    private static LambdaExpression BuildInputProjection(Type inputType)
    {
        var p = Expression.Parameter(inputType, "x");
        var props = new[] { "Open", "High", "Low", "KsqlTimeFrameClose" }
            .Select(n => inputType.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase))
            .Where(pr => pr != null)
            .Cast<PropertyInfo>()
            .ToArray();
        if (props.Length == 0) return Expression.Lambda(p, p);
        var bindings = props.Select(pr => Expression.Bind(pr, Expression.Property(p, pr)));
        var body = Expression.MemberInit(Expression.New(inputType), bindings);
        return Expression.Lambda(body, p);
    }
}
