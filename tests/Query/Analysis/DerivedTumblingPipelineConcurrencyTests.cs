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
class ConcurrencySource
{
    public int Id { get; set; }
}

public class DerivedTumblingPipelineConcurrencyTests
{
    [Fact]
    public async Task RunAsync_registers_all_models_without_conflict()
    {
        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(1, "m"), new Timeframe(5, "m") },
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
        var baseModel = new EntityModel { EntityType = typeof(ConcurrencySource) };
        var model = new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(ConcurrencySource) },
            Windows = { "1m", "5m" }
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

        var expected = 5; // 1s hub + 1m: Live + 5m: Live
        Assert.Equal(expected, registry.Count);
        Assert.Equal(expected, ddls.Count);
        var finals = ddls.Where(d => d.Contains("_final") && !d.Contains("_final_s")).ToList();
        Assert.Single(finals);
        Assert.Contains("EMIT FINAL", finals[0]);
    }
}
