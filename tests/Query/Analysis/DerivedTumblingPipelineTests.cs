using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
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
    public async Task Live_and_Final_emit_different_ddl_without_mutating_model()
    {
        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(1, "m") },
            Keys = new[] { "Id", "BucketStart" },
            Projection = new[] { "Id", "BucketStart", "KsqlTimeFrameClose" },
            PocoShape = new[]
            {
                new ColumnShape("Id", typeof(int), false),
                new ColumnShape("BucketStart", typeof(long), false),
                new ColumnShape("KsqlTimeFrameClose", typeof(double), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Id", "BucketStart" }, string.Empty, "KsqlTimeFrameClose", string.Empty),
            WeekAnchor = DayOfWeek.Monday
        };
        var baseModel = new EntityModel { EntityType = typeof(TestSource) };
        var model = new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(TestSource) },
            Windows = { "1m" }
        };

        var ddls = new ConcurrentBag<string>();
        Task Exec(string sql)
        {
            ddls.Add(sql);
            return Task.CompletedTask;
        }

        var mapping = new MappingRegistry();
        var registry = new ConcurrentDictionary<Type, EntityModel>();
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("dyn"), AssemblyBuilderAccess.Run);
        var mod = asm.DefineDynamicModule("m");
        Type Resolver(string _) => mod.DefineType("T" + Guid.NewGuid().ToString("N")).CreateType()!;

        await DerivedTumblingPipeline.RunAsync(qao, baseModel, model, Exec, Resolver, mapping, registry, new LoggerFactory().CreateLogger("test"));

        var live = ddls.Single(s => s.Contains("_1m_live"));
        var final = ddls.Single(s => s.Contains("_1m_final"));
        Assert.Contains(ddls, s => s.Contains("_prev_1m"));
        Assert.Contains("EMIT CHANGES", live);
        Assert.Contains("EMIT FINAL", final);
    }
}
