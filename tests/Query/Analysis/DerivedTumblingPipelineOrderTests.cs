using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class DerivedTumblingPipelineOrderTests
{
    [KsqlTopic("foo")]
    public class TestSource
    {
        public int Id { get; set; }
    }

    [Fact]
    public async Task Emits_DDL_in_expected_order()
    {
        var qao = new TumblingQao {
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(1,"m"), new Timeframe(5,"m") },
            Keys = new[] { "Id", "BucketStart" },
            Projection = new[] { "Id", "BucketStart", "KsqlTimeFrameClose" },
            PocoShape = new[] {
                new ColumnShape("Id", typeof(int), false),
                new ColumnShape("BucketStart", typeof(long), false),
                new ColumnShape("KsqlTimeFrameClose", typeof(double), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Id", "BucketStart" }, string.Empty, "KsqlTimeFrameClose", string.Empty),
            WeekAnchor = DayOfWeek.Monday
        };
        var baseModel = new EntityModel { EntityType = typeof(TestSource) };
        var model = new KsqlQueryModel { SourceTypes = new[] { typeof(TestSource) }, Windows = { "1m", "5m" } };

        var order = new List<string>();
        Task Exec(string sql) { order.Add(sql); return Task.CompletedTask; }

        await DerivedTumblingPipeline.RunAsync(qao, baseModel, model, Exec,
            _ => typeof(object), new MappingRegistry(), new(), NullLoggerFactory.Instance.CreateLogger("test"));

        Assert.Collection(order,
            ddl => Assert.StartsWith("CREATE TABLE foo_1s_final", ddl),
            ddl => Assert.StartsWith("CREATE STREAM foo_1s_final_s", ddl),
            ddl => Assert.StartsWith("CREATE TABLE foo_hb_1s", ddl),
            ddl => Assert.StartsWith("CREATE TABLE foo_1m_live", ddl),
            ddl => Assert.StartsWith("CREATE TABLE foo_5m_live", ddl)
        );
    }
}

