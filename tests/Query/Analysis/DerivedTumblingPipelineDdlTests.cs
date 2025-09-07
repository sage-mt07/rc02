using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Dsl;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class DerivedTumblingPipelineDdlTests
{
    public class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime BucketStart { get; set; }
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
            .Tumbling(r => r.Timestamp, new[] { 1 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new
            {
                g.Key.Broker,
                g.Key.Symbol,
                g.Key.BucketStart,
                Open = g.Max(x => x.Open)
            }))).Body;

    [Fact]
    public async Task Builds_Expected_Ddl_For_Roles()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var model = new KsqlQueryRoot()
            .From<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new { g.Key.Broker, g.Key.Symbol, g.Key.BucketStart, Open = g.Max(x => x.Open) })
            .AsPush()
            .Build();

        var ddls = new List<string>();
        await DerivedTumblingPipeline.RunAsync(
            qao,
            model,
            sql => { ddls.Add(sql); return Task.CompletedTask; },
            _ => typeof(object),
            new Dictionary<Type, EntityModel>(),
            NullLogger.Instance);

        var live = ddls.First(s => s.Contains("bar_1m_live"));
        Assert.Contains("EMIT CHANGES", live);

        var final = ddls.First(s => s.Contains("bar_1m_final"));
        Assert.Contains("EMIT FINAL", final);
        Assert.Contains("WINDOWSTART AS BucketStart", final);

        var agg = ddls.First(s => s.Contains("bar_1m_agg_final"));
        Assert.Contains("EMIT FINAL", agg);
    }
}
