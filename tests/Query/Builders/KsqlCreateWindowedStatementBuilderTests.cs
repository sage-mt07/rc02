using System;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class KsqlCreateWindowedStatementBuilderTests
{
    private class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double Bid { get; set; }
    }

    [Fact]
    public void Build_Includes_Window_Tumbling_1m()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart, Open = g.EarliestByOffset(x => x.Bid) })
            .AsPush()
            .Build();

        var sql = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.Build("bar_1m_live", model, "1m");
        Assert.Contains("WINDOW TUMBLING (SIZE 1 MINUTES)", sql);
        Assert.Contains("CREATE TABLE bar_1m_live", sql);
    }

    [Fact]
    public void BuildAll_Generates_Per_Window()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1, 5 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart, Open = g.EarliestByOffset(x => x.Bid) })
            .AsPush()
            .Build();

        var map = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.BuildAll(
            "bar",
            model,
            tf => $"bar_{tf}_live");

        Assert.True(map.ContainsKey("1m"));
        Assert.True(map.ContainsKey("5m"));
        Assert.Contains("bar_1m_live", map["1m"]);
        Assert.Contains("bar_5m_live", map["5m"]);
    }
}

