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
            var allow = role switch
            {
                Role.Final1sStream or Role.Final1s => tf == "1s",
                Role.Prev1m => tf == "1m",
                Role.Live => true,
                Role.Hb => true,
                Role.Fill => true,
                _ => true
            };
            if (!allow)
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
        if (role == Role.Fill)
        {
            // Experimental: Build Fill DDL by driving from HB and left-joining live.
            // Note: prev_1m join and filler specifics will be added in a later pass.
            var keys = model.AdditionalSettings.TryGetValue("keys", out var kObj) ? (string[])kObj! : Array.Empty<string>();
            var projection = model.AdditionalSettings.TryGetValue("projection", out var pObj) ? (string[])pObj! : Array.Empty<string>();
            var bucketCol = queryModel.BucketColumnName ?? throw new InvalidOperationException("WhenEmpty/Fill requires WindowStart() in Select to define the bucket column.");
            var hbName = $"{baseName}_hb_{tf}";
            var liveName = $"{baseName}_{tf}_live";
            // Optionally include prev_1m when timeframe is 1m to enable previous-close fill
            string? prevName = tf.Equals("1m", StringComparison.OrdinalIgnoreCase) ? $"{baseName}_prev_1m" : null;
            ddl = KsqlFillStatementBuilder.Build(name, keys, projection, bucketCol, hbName, liveName, prevName);
            if (!string.IsNullOrWhiteSpace(emit) && !ddl.Contains("EMIT ", StringComparison.OrdinalIgnoreCase))
                ddl = ddl.Replace(";", $" {emit};");
        }
        else if (role == Role.Prev1m)
        {
            var keys = model.AdditionalSettings.TryGetValue("keys", out var kObj2) ? (string[])kObj2! : Array.Empty<string>();
            var projection2 = model.AdditionalSettings.TryGetValue("projection", out var pObj2) ? (string[])pObj2! : Array.Empty<string>();
            var bucketCol2 = queryModel.BucketColumnName ?? throw new InvalidOperationException("Prev requires WindowStart() in Select to define the bucket column.");
            var hbName2 = $"{baseName}_hb_{tf}"; // tf should be 1m
            var liveName2 = $"{baseName}_{tf}_live";
            ddl = KsqlPrevStatementBuilder.Build(name, keys, projection2, bucketCol2, hbName2, liveName2, 1);
            if (!string.IsNullOrWhiteSpace(emit) && !ddl.Contains("EMIT ", StringComparison.OrdinalIgnoreCase))
                ddl = ddl.Replace(";", $" {emit};");
        }
        else if (role == Role.Final1sStream)
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
        // App-agnostic: select all columns (identity -> SELECT *)
        var p = Expression.Parameter(inputType, "x");
        return Expression.Lambda(p, p);
    }
}
