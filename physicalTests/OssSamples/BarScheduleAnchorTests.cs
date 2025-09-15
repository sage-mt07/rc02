using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Builders;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// OnModelCreating ベースの週次（日曜アンカー）モデル→クエリ→マテリアライズ→検証。
/// </summary>
public class BarScheduleAnchorTests
{
    private class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Bid { get; set; }
    }

    private class Bar1wkLive
    {
        [KsqlKey(1)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(2)] public string Symbol { get; set; } = string.Empty;
        [KsqlKey(3)] public DateTime BucketStart { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double KsqlTimeFrameClose { get; set; }
    }

    private sealed class TestContext : KsqlContext
    {
        public TestContext() : base(new KsqlDslOptions()) { }
        protected override bool SkipSchemaRegistration => true;
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rate>(readOnly: true);
            //modelBuilder.Entity<Bar1wkLive>()
            //    .ToQuery(q => q.From<Rate>()
            //        .Tumbling(r => r.Timestamp, week: DayOfWeek.Sunday)
            //        .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            //        .Select(g => new Bar1wkLive
            //        {
            //            Broker = g.Key.Broker,
            //            Symbol = g.Key.Symbol,
            //            BucketStart = g.Key.BucketStart,
            //            Open = g.EarliestByOffset(x => x.Bid),
            //            High = g.Max(x => x.Bid),
            //            Low = g.Min(x => x.Bid),
            //            KsqlTimeFrameClose = g.LatestByOffset(x => x.Bid)
            //        })
            //        .AsPush());
        }
    }

    [Fact]
    public void Weekly_SundayAnchor_Materialize_And_Verify()
    {
        var ctx = new TestContext();
        var em = ctx.GetEntityModels()[typeof(Bar1wkLive)];
        var model = em.QueryModel!;
        Assert.Contains("1wk", model.Windows);
        Assert.Equal(DayOfWeek.Sunday, model.WeekAnchor);
        var sql = KsqlCreateStatementBuilder.Build("bar_1wk_live", model);
        Assert.Contains("EARLIEST_BY_OFFSET(Bid)", sql);
        Assert.Contains("LATEST_BY_OFFSET(Bid)", sql);
    }
}
