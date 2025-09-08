using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class DerivationPlannerTests
{
    [KsqlTopic("bar")]
    private class Source
    {
        public int Id { get; set; }
    }

    [KsqlTopic("bar")]
    private class SourceOhlc
    {
        public int Id { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        [KsqlTimeFrameClose]
        public double KsqlTimeFrameClose { get; set; }
    }

    private static TumblingQao Create(Timeframe tf) => new()
    {
        TimeKey = "Timestamp",
        Windows = new[] { tf },
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

    [Fact]
    public void Plan_1m_Includes_Agg_Live_Final_Hb()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_1m_agg_final" && e.Role == Role.AggFinal);
        Assert.Contains(entities, e => e.Id == "bar_1m_live" && e.Role == Role.Live);
        Assert.Contains(entities, e => e.Id == "bar_1m_final" && e.Role == Role.Final);
        Assert.DoesNotContain(entities, e => e.Role == Role.Prev1m);
        Assert.Contains(entities, e => e.Id == "bar_hb_1m" && e.Role == Role.Hb);
    }

    [Fact]
    public void Plan_5m_Excludes_Hb()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(5, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_5m_agg_final" && e.Role == Role.AggFinal);
        Assert.Contains(entities, e => e.Id == "bar_5m_live" && e.Role == Role.Live);
        Assert.Contains(entities, e => e.Id == "bar_5m_final" && e.Role == Role.Final);
        Assert.DoesNotContain(entities, e => e.Role == Role.Hb);
    }
}
