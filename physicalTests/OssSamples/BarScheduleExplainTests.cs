using System;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// Physical tests for day/week bars with market schedule awareness.
/// Uses EXPLAIN to validate ksql acceptance. Event-time is provided via TS column.
/// </summary>
public class BarScheduleExplainTests
{
    private sealed class SimpleContext : KsqlContext
    {
        public SimpleContext(KsqlDslOptions opt) : base(opt) { }
        protected override bool SkipSchemaRegistration => true;
        protected override void OnModelCreating(IModelBuilder modelBuilder) { }
    }

    private static async Task<KsqlContext> CreateReadyContextAsync()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = "localhost:9092" },
            SchemaRegistry = new Kafka.Ksql.Linq.Core.Configuration.SchemaRegistrySection { Url = "http://localhost:8081" },
            KsqlDbUrl = "http://localhost:8088"
        };
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync(options.KsqlDbUrl!, TimeSpan.FromSeconds(120));
        return new SimpleContext(options);
    }

    private static async Task EnsureSourcesAsync(KsqlContext ctx)
    {
        // Rates with explicit timestamp column for event-time windowing
        var createRates = @"CREATE STREAM IF NOT EXISTS DEDUPRATES (
  BROKER VARCHAR KEY,
  SYMBOL VARCHAR,
  TS BIGINT,
  BID DOUBLE
) WITH (KAFKA_TOPIC='deduprates', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1, TIMESTAMP='TS');";
        var r = await ctx.ExecuteStatementAsync(createRates);
        Assert.True(r.IsSuccess, r.Message);

        // Minimal market schedule stream (open/close expressed in epoch millis)
        var createSched = @"CREATE STREAM IF NOT EXISTS MSCHED (
  BROKER VARCHAR KEY,
  SYMBOL VARCHAR,
  OPEN_TS BIGINT,
  CLOSE_TS BIGINT
) WITH (KAFKA_TOPIC='msched', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1);";
        var s = await ctx.ExecuteStatementAsync(createSched);
        Assert.True(s.IsSuccess, s.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Explain_Daily_With_MarketSchedule_ShouldBeAccepted()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourcesAsync(ctx);

        var sql = @"SELECT
  D.BROKER,
  D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D
JOIN MSCHED S WITHIN 1 DAYS ON (D.BROKER = S.BROKER)
WINDOW TUMBLING (SIZE 1 DAY)
WHERE D.SYMBOL = S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS
GROUP BY D.BROKER, D.SYMBOL
EMIT CHANGES;";

        var res = await ctx.ExecuteExplainAsync(sql);
        Assert.True(res.IsSuccess, res.Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Explain_Weekly_With_MarketSchedule_ShouldBeAccepted()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourcesAsync(ctx);

        var sql = @"SELECT
  D.BROKER,
  D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D
JOIN MSCHED S WITHIN 7 DAYS ON (D.BROKER = S.BROKER)
WINDOW TUMBLING (SIZE 7 DAYS)
WHERE D.SYMBOL = S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS
GROUP BY D.BROKER, D.SYMBOL
EMIT CHANGES;";

        var res = await ctx.ExecuteExplainAsync(sql);
        Assert.True(res.IsSuccess, res.Message);
    }
}
