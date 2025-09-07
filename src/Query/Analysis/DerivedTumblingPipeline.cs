using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivedTumblingPipeline
{
    public static async Task RunAsync(
        TumblingQao qao,
        KsqlQueryModel queryModel,
        Func<string, Task> execute,
        Func<string, Type> resolveType,
        IDictionary<Type, EntityModel> registry,
        ILogger logger)
    {
        var entities = PlanDerivedEntities(qao);
        var models = AdaptModels(entities);
        foreach (var m in models)
        {
            var role = Enum.Parse<Role>((string)m.AdditionalSettings["role"]);
            await BuildDdlAndRegister(queryModel, m, role, execute, resolveType, registry, logger);
        }
    }

    public static IReadOnlyList<DerivedEntity> PlanDerivedEntities(TumblingQao qao)
        => DerivationPlanner.Plan(qao);

    public static IReadOnlyList<EntityModel> AdaptModels(IReadOnlyList<DerivedEntity> entities)
        => EntityModelAdapter.Adapt(entities);

    private static async Task BuildDdlAndRegister(
        KsqlQueryModel queryModel,
        EntityModel model,
        Role role,
        Func<string, Task> execute,
        Func<string, Type> resolveType,
        IDictionary<Type, EntityModel> registry,
        ILogger logger)
    {
        var tf = (string)model.AdditionalSettings["timeframe"];
        var name = (string)model.AdditionalSettings["id"];
        var orig = queryModel.IsFinal;
        queryModel.IsFinal = role is Role.Final or Role.AggFinal;
        var ddl = KsqlCreateWindowedStatementBuilder.Build(name, queryModel, tf);
        queryModel.IsFinal = orig;
        logger.LogInformation("KSQL DDL (derived {Entity}): {Sql}", name, ddl);
        await execute(ddl);
        var dt = resolveType(name);
        model.EntityType = dt;
        model.TopicName = name;
        model.SetStreamTableType(StreamTableType.Table);
        registry[dt] = model;
    }
}
