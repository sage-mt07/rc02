using System;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Dsl;
using Xunit;
using Kafka.Ksql.Linq.Core.Modeling;

namespace Kafka.Ksql.Linq.Tests.Integration;

public class BarWeekAnchorCompareTests
{
    private class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Bid { get; set; }
    }

    private class BarWkSun
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlKey(3)] public DateTime BucketStart { get; set; }
        public double Last { get; set; }
    }

    private class BarWkMon
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlKey(3)] public DateTime BucketStart { get; set; }
        public double Last { get; set; }
    }

    private sealed class TestContext : KsqlContext
    {
        public TestContext() : base(new KsqlDslOptions()) { }
        protected override bool SkipSchemaRegistration => true;
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rate>(readOnly: true);

            modelBuilder.Entity<BarWkSun>()
                .ToQuery(q => q.From<Rate>()
                    .Tumbling(r => r.Timestamp, week: DayOfWeek.Sunday)
                    .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
                    .Select(g => new BarWkSun
                    {
                        Broker = g.Key.Broker,
                        Symbol = g.Key.Symbol,
                        BucketStart = g.Key.BucketStart,
                        Last = g.LatestByOffset(x => x.Bid)
                    })
                    .AsPush());

            modelBuilder.Entity<BarWkMon>()
                .ToQuery(q => q.From<Rate>()
                    .Tumbling(r => r.Timestamp, week: DayOfWeek.Monday)
                    .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
                    .Select(g => new BarWkMon
                    {
                        Broker = g.Key.Broker,
                        Symbol = g.Key.Symbol,
                        BucketStart = g.Key.BucketStart,
                        Last = g.LatestByOffset(x => x.Bid)
                    })
                    .AsPush());
        }
    }

    [Fact]
    public void Compare_SundayVsMonday_Weekly_ModelDiffers()
    {
        var ctx = new TestContext();
        var models = ctx.GetEntityModels();
        var sun = models[typeof(BarWkSun)].QueryModel!;
        var mon = models[typeof(BarWkMon)].QueryModel!;

        Assert.Contains("1wk", sun.Windows);
        Assert.Contains("1wk", mon.Windows);
        Assert.Equal(DayOfWeek.Sunday, sun.WeekAnchor);
        Assert.Equal(DayOfWeek.Monday, mon.WeekAnchor);
    }
}
