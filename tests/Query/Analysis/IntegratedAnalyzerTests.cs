using System;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class IntegratedAnalyzerTests
{
    public class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class MarketSchedule
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime MarketDate { get; set; }
    }

    private static Expression BuildExpression() =>
        ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>(
                (r, s) =>
                    r.Broker == s.Broker &&
                    r.Symbol == s.Symbol &&
                    s.Open <= r.Timestamp &&
                    r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1, 5 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new
            {
                g.Key.Broker,
                g.Key.Symbol,
                g.Key.BucketStart,
                Open = default(decimal),
                High = default(decimal),
                Low = default(decimal),
                Close = default(decimal)
            }))).Body;

    [Fact]
    public void Analyzer_Extracts_Tumbling_And_TimeFrame_InclusiveFlags()
    {
        var analysis = Analyze(BuildExpression());
        Assert.Equal("Timestamp", analysis.TimeKey);
        Assert.Equal(new[] { "1m", "5m" }, analysis.Windows);
        Assert.True(analysis.BasedOnOpenInclusive);
        Assert.False(analysis.BasedOnCloseInclusive);
    }

    [Fact]
    public void Analyzer_Fails_When_NoWindows_Or_TimeKeyMissing()
    {
        Expression noWindows = ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>((r, s) => r.Broker == s.Broker,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, null, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(x => x))).Body;
        Assert.Throws<InvalidOperationException>(() => Analyze(noWindows));

        Expression missingTimeKey = ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>((r, s) => s.Open <= r.Timestamp && r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol })
            .Select(x => x))).Body;
        Assert.Throws<InvalidOperationException>(() => Analyze(missingTimeKey));
    }

    private static ExpressionAnalysisResult Analyze(Expression expr)
    {
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(expr);
        var result = visitor.Result;
        if (result.Windows.Count == 0)
            throw new InvalidOperationException("Tumbling windows are required");
        if (result.TimeKey == null)
            throw new InvalidOperationException("Time key is required");
        if (!result.GroupByKeys.Contains(result.TimeKey) && !result.GroupByKeys.Contains("BucketStart"))
            throw new InvalidOperationException("Time key must be part of GroupBy keys");
        return result;
    }
}
