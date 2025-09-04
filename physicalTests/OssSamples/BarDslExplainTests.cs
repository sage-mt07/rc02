using System;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Configuration;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// Physical tests inspired by docs/chart.md to validate that ksqlDB accepts
/// tumbling-window OHLC style queries for stream/stream style aggregation.
/// This focuses on EXPLAIN validation (syntax/plan) rather than end-to-end data.
/// </summary>
public class BarDslExplainTests
{
    private static async Task<KsqlContext> CreateReadyContextAsync()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = "localhost:9092" },
            SchemaRegistry = new SchemaRegistrySection { Url = "http://localhost:8081" },
            KsqlDbUrl = "http://localhost:8088"
        };
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync(options.KsqlDbUrl!, TimeSpan.FromSeconds(120));
        return new SimpleContext(options);
    }

    private sealed class SimpleContext : KsqlContext
    {
        public SimpleContext(KsqlDslOptions options) : base(options) { }
        protected override bool SkipSchemaRegistration => true; // we'll use explicit KSQL statements
        protected override void OnModelCreating(Kafka.Ksql.Linq.Core.Abstractions.IModelBuilder modelBuilder) { }
    }

    private static async Task EnsureSourceAsync(KsqlContext ctx)
    {
        var createSrc = @"CREATE STREAM IF NOT EXISTS DEDUPRATES (
  BROKER VARCHAR KEY,
  SYMBOL VARCHAR,
  TS BIGINT,
  BID DOUBLE
) WITH (KAFKA_TOPIC='deduprates', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1);";
        var cr = await ctx.ExecuteStatementAsync(createSrc);
        Assert.True(cr.IsSuccess, cr.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Explain_Tumbling_1m_Live_Ohlc_ShouldBeAccepted()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourceAsync(ctx);

        // Explain a 1-minute tumbling OHLC live query using ksqlDB aggregate functions
        var select = @"SELECT
  BROKER,
  SYMBOL,
  EARLIEST_BY_OFFSET(BID) AS OPEN,
  MAX(BID) AS HIGH,
  MIN(BID) AS LOW,
  LATEST_BY_OFFSET(BID) AS CLOSE
FROM DEDUPRATES
WINDOW TUMBLING (SIZE 1 MINUTES)
GROUP BY BROKER, SYMBOL
EMIT CHANGES;";

        var res = await ctx.ExecuteExplainAsync(select);
        Assert.True(res.IsSuccess, res.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Explain_Tumbling_5m_Live_Ohlc_ShouldBeAccepted()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourceAsync(ctx);

        var select = @"SELECT
  BROKER,
  SYMBOL,
  EARLIEST_BY_OFFSET(BID) AS OPEN,
  MAX(BID) AS HIGH,
  MIN(BID) AS LOW,
  LATEST_BY_OFFSET(BID) AS CLOSE
FROM DEDUPRATES
WINDOW TUMBLING (SIZE 5 MINUTES)
GROUP BY BROKER, SYMBOL
EMIT CHANGES;";

        var res = await ctx.ExecuteExplainAsync(select);
        Assert.True(res.IsSuccess, res.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Explain_Tumbling_1m_Final_Ohlc_ShouldBeAccepted()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourceAsync(ctx);

        // ksqlDB: EMIT FINAL requires windowed aggregation; validate acceptance
        var select = @"SELECT
  BROKER,
  SYMBOL,
  EARLIEST_BY_OFFSET(BID) AS OPEN,
  MAX(BID) AS HIGH,
  MIN(BID) AS LOW,
  LATEST_BY_OFFSET(BID) AS CLOSE
FROM DEDUPRATES
WINDOW TUMBLING (SIZE 1 MINUTES, GRACE PERIOD 2 MINUTES)
GROUP BY BROKER, SYMBOL
EMIT FINAL;";

        var res = await ctx.ExecuteExplainAsync(select);
        Assert.True(res.IsSuccess, res.Message);
    }
}
