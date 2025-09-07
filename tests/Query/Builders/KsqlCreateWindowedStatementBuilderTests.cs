using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Kafka.Ksql.Linq.Query.Abstractions;
using System;
using System.Linq.Expressions;
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

    [KsqlTable]
    private class RateTable
    {
        [KsqlKey(0)] public string Broker { get; set; } = string.Empty;
        [KsqlKey(1)] public string Symbol { get; set; } = string.Empty;
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

    [Fact]
    public void Build_Final_1m_Emits_Windowstart_Arrow_And_Final()
    {
        var model = new KsqlQueryRoot()
            .From<RateTable>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, Open = g.EarliestByOffset(x => x.Bid) })
            .AsFinal()
            .AsPush()
            .Build();
        var sql = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.Build("bar_1m_final", model, "1m");
        Assert.Contains("WINDOW TUMBLING (SIZE 1 MINUTES)", sql);
        Assert.Contains("SELECT WINDOWSTART AS BucketStart", sql);
        Assert.Contains("EMIT FINAL", sql);
        Assert.Contains("GROUP BY KEY->BROKER, KEY->SYMBOL", sql);
        Assert.Contains("KEY->BROKER AS Broker, KEY->SYMBOL AS Symbol", sql);
        Assert.DoesNotContain("COMPOSE(", sql);
    }

    [Fact]
    public void Build_Final_5m_Emits_Windowstart_Arrow_And_Final()
    {
        var model = new KsqlQueryRoot()
            .From<RateTable>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1, 5 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, Open = g.EarliestByOffset(x => x.Bid) })
            .AsFinal()
            .AsPush()
            .Build();

        var map = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.BuildAll(
            "bar",
            model,
            tf => $"bar_{tf}_final");
        var sql = map["5m"];
        Assert.Contains("WINDOW TUMBLING (SIZE 5 MINUTES)", sql);
        Assert.Contains("SELECT WINDOWSTART AS BucketStart", sql);
        Assert.Contains("EMIT FINAL", sql);
        Assert.Contains("GROUP BY KEY->BROKER, KEY->SYMBOL", sql);
        Assert.Contains("KEY->BROKER AS Broker, KEY->SYMBOL AS Symbol", sql);
        Assert.DoesNotContain("COMPOSE(", sql);
    }

    [Fact]
    public void Build_Final_StreamTableJoin_Applies_Arrow_To_Table_Side()
    {
        var model = new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(RateTable), typeof(Rate) },
            JoinCondition = (Expression<Func<RateTable, Rate, bool>>)((t, s) => t.Broker == s.Broker && t.Symbol == s.Symbol),
            WhereCondition = (Expression<Func<RateTable, Rate, bool>>)((t, s) => t.Broker != string.Empty && s.Symbol != string.Empty),
            GroupByExpression = (Expression<Func<RateTable, Rate, object>>)((t, s) => new { t.Broker, s.Symbol }),
            SelectProjection = (Expression<Func<RateTable, Rate, object>>)((t, s) => new { t.Broker, s.Symbol }),
            IsFinal = true,
            ExecutionMode = QueryExecutionMode.PushQuery
        };
        model.Windows.Add("1m");

        var sql = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.Build("mix_1m_final", model, "1m");
        Assert.Contains("SELECT WINDOWSTART AS BucketStart, KEY->BROKER AS Broker, i.Symbol AS Symbol", sql);
        Assert.Contains("GROUP BY KEY->BROKER, Symbol", sql);
        Assert.Contains("WHERE ((KEY->BROKER != Empty) AND (i.Symbol != Empty))", sql);
        Assert.DoesNotContain("KEY->SYMBOL", sql);
        Assert.DoesNotContain("COMPOSE(", sql);
    }

    [Fact]
    public void Build_NoWindow_Creates_Stream()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Select(r => r)
            .AsPush()
            .Build();

        var sql = Kafka.Ksql.Linq.Query.Builders.KsqlCreateStatementBuilder.Build("rates", model);
        Assert.StartsWith("CREATE STREAM rates", sql);
    }

    [Fact]
    public void Build_WithWindow_Creates_Table()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart, Open = g.EarliestByOffset(x => x.Bid) })
            .AsPush()
            .Build();

        var sql = Kafka.Ksql.Linq.Query.Builders.KsqlCreateWindowedStatementBuilder.Build("bar_1m", model, "1m");
        Assert.StartsWith("CREATE TABLE bar_1m", sql);
        Assert.Contains("WINDOW TUMBLING", sql);
    }

    [Fact]
    public void DetermineType_Tumbling_Returns_Table()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart })
            .AsPush()
            .Build();
        Assert.Equal(StreamTableType.Table, model.DetermineType());
    }

    [Fact]
    public void DetermineType_NoAggregation_Returns_Stream()
    {
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Select(r => r)
            .AsPush()
            .Build();
        Assert.Equal(StreamTableType.Stream, model.DetermineType());
    }
}

