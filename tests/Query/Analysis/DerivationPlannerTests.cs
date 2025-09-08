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

    private static TumblingQao Create(params Timeframe[] tfs) => new()
    {
        TimeKey = "Timestamp",
        Windows = tfs,
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
    public void Plan_1m_Includes_Agg_Live_Final_Hb_Prev()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_1m_agg_final" && e.Role == Role.AggFinal);
        var live = Assert.Single(entities, e => e.Id == "bar_1m_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", live.InputHint);
        Assert.Equal("BAR_HB_1M", live.SyncHint);
        var final = Assert.Single(entities, e => e.Id == "bar_1m_final" && e.Role == Role.Final);
        Assert.Equal("bar_1m_agg_final", final.InputHint);
        Assert.Equal("BAR_HB_1M", final.SyncHint);
        Assert.Equal("bar_prev_1m", final.PrevHint);
        var prev = Assert.Single(entities, e => e.Id == "bar_prev_1m" && e.Role == Role.Prev1m);
        Assert.Equal("bar_1m_final", prev.InputHint);
        Assert.Equal("BAR_HB_1M", prev.SyncHint);
        Assert.Contains(entities, e => e.Id == "bar_hb_1m" && e.Role == Role.Hb);
    }

    [Fact]
    public void Plan_5m_Includes_Hb()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(5, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_5m_agg_final" && e.Role == Role.AggFinal);
        var live5 = Assert.Single(entities, e => e.Id == "bar_5m_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", live5.InputHint);
        Assert.Equal("BAR_HB_5M", live5.SyncHint);
        var final = Assert.Single(entities, e => e.Id == "bar_5m_final" && e.Role == Role.Final);
        Assert.Equal("bar_5m_agg_final", final.InputHint);
        Assert.Equal("BAR_HB_5M", final.SyncHint);
        Assert.Equal("bar_prev_1m", final.PrevHint);
        Assert.Contains(entities, e => e.Id == "bar_hb_5m" && e.Role == Role.Hb);
        Assert.Contains(entities, e => e.Id == "bar_hb_1m" && e.Role == Role.Hb);
        Assert.Contains(entities, e => e.Id == "bar_prev_1m" && e.Role == Role.Prev1m);
    }

    [Fact]
    public void Plan_1wk_Live_Uses_1d_Live_Input()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "wk")), model);

        var live = Assert.Single(entities, e => e.Id == "bar_1wk_live" && e.Role == Role.Live);
        Assert.Equal("bar_1d_live", live.InputHint);
    }

    [Fact]
    public void Plan_Hour_Windows_Chain()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(3, "h"), new Timeframe(1, "h")), model);

        var live1 = Assert.Single(entities, e => e.Id == "bar_1h_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", live1.InputHint);
        var live3 = Assert.Single(entities, e => e.Id == "bar_3h_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", live3.InputHint);
    }

    [Fact]
    public void Plan_Day_Windows_Chain()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "d"), new Timeframe(1, "h")), model);

        var liveH = Assert.Single(entities, e => e.Id == "bar_1h_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", liveH.InputHint);
        var liveD = Assert.Single(entities, e => e.Id == "bar_1d_live" && e.Role == Role.Live);
        Assert.Equal("bar_1m_live", liveD.InputHint);
    }

    [Fact]
    public void Plan_WhenEmpty_Adds_Fill_Entity()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "m")), model, true);
        Assert.Contains(entities, e => e.Id == "bar_1m_fill" && e.Role == Role.Fill);
    }
}
