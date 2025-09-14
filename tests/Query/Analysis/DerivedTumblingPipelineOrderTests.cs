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

        // Enable WhenEmpty path to include HB per timeframe
        model.WhenEmptyFiller = (System.Linq.Expressions.Expression<System.Func<int,int,int>>)((a,b) => a);
        await DerivedTumblingPipeline.RunAsync(qao, baseModel, model, Exec,
            _ => typeof(object), new MappingRegistry(), new(), NullLoggerFactory.Instance.CreateLogger("test"));

        // Validate relative order of key DDLs; additional DDLs (Fill/Prev etc.) may exist
        int idx(string prefix) => order.FindIndex(s => s.StartsWith(prefix, StringComparison.Ordinal));
        var i0 = idx("CREATE STREAM foo_1s_final_s");
        var i1 = idx("CREATE TABLE foo_1s_final");
        var i2 = idx("CREATE TABLE foo_hb_1s");
        var i3 = idx("CREATE TABLE foo_1m_live");
        var i4 = idx("CREATE TABLE foo_5m_live");
        var i5 = idx("CREATE TABLE foo_hb_5m");
        Assert.True(i0 >= 0 && i1 > i0 && i2 > i1 && i3 > i2 && i4 > i3 && i5 > i4, string.Join("\n", order));
    }
}

