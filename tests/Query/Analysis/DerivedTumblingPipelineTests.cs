using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

[KsqlTopic("test-topic")]
class TestSource
{
    public int Id { get; set; }
}

public class DerivedTumblingPipelineTests
{
    [Fact]
    public async Task DerivedTopicsUseBaseTopicName()
    {
        var qao = new TumblingQao
        {
            BaseTopicName = typeof(TestSource).GetCustomAttribute<KsqlTopicAttribute>()!.Name,
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(5, "m") },
            Keys = new[] { "Id" },
            Projection = new[] { "Id" },
            PocoShape = new[] { new ColumnShape("Id", typeof(int), false) },
            BasedOn = new BasedOnSpec(new[] { "Id" }, string.Empty, string.Empty, string.Empty),
            WeekAnchor = DayOfWeek.Monday
        };
        var model = new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(TestSource) },
            HasTumbling = true,
            Windows = { "5m" }
        };
        var registry = new Dictionary<Type, EntityModel>();
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dyn"), AssemblyBuilderAccess.Run);
        var mod = asm.DefineDynamicModule("m");
        Type Resolver(string n) => mod.DefineType("T" + Guid.NewGuid().ToString("N")).CreateType()!;
        await DerivedTumblingPipeline.RunAsync(
            qao,
            model,
            _ => Task.CompletedTask,
            Resolver,
            registry,
            new LoggerFactory().CreateLogger("test"));
        var topics = new List<string>();
        foreach (var em in registry.Values)
            topics.Add(em.TopicName!);
        Assert.Contains("test-topic_5m_live", topics);
        Assert.Contains("test-topic_5m_final", topics);
    }
}
