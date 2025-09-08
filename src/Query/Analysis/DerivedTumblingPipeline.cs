using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
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
        await Parallel.ForEachAsync(models, async (m, _) =>
        {
            var role = Enum.Parse<Role>((string)m.AdditionalSettings["role"]);
            var (ddl, dt, ns) = BuildDdlAndRegister(baseName, queryModel, m, role, resolveType);
            logger.LogInformation("KSQL DDL (derived {Entity}): {Sql}", m.TopicName, ddl);
            await execute(ddl);
            mapping.RegisterEntityModel(m, genericValue: true, overrideNamespace: ns);
            registry[dt] = m;
        });
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
        qm.IsFinal = role is Role.Final or Role.AggFinal;
        var tf = (string)model.AdditionalSettings["timeframe"];
        var name = role switch
        {
            Role.AggFinal => $"{baseName}_{tf}_agg_final",
            Role.Live => $"{baseName}_{tf}_live",
            Role.Final => $"{baseName}_{tf}_final",
            Role.Prev1m => $"{baseName}_prev_1m",
            Role.Hb => $"{baseName}_hb_{tf}",
            Role.Fill => $"{baseName}_{tf}_fill",
            _ => $"{baseName}_{tf}"
        };
        var ddl = KsqlCreateWindowedStatementBuilder.Build(name, qm, tf);
        var dt = resolveType(name);
        model.EntityType = dt;
        model.TopicName = name;
        model.SetStreamTableType(qm.DetermineType());
        var ns = model.AdditionalSettings.TryGetValue("namespace", out var nsObj) ? nsObj?.ToString() : null;
        return (ddl, dt, ns);
    }
}
