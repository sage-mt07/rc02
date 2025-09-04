using System;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Xunit;
using System.Threading;

namespace Kafka.Ksql.Linq.Tests.Integration;

/// <summary>
/// Weekly bar with Sunday anchor simulated via schedule (OPEN_TS at Sunday 00:00).
/// Verifies CSAS success and row availability.
/// </summary>
public class BarScheduleAnchorTests
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

    private static long Ms(string isoUtc)
        => (long)(DateTime.Parse(isoUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal)
            - DateTime.UnixEpoch).TotalMilliseconds;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Weekly_SundayAnchor_CsasAndCount_ShouldSucceed()
    {
        await using var ctx = await CreateReadyContextAsync();
        // Sources
        await ctx.ExecuteStatementAsync(@"CREATE STREAM IF NOT EXISTS DEDUPRATES (
  BROKER VARCHAR KEY, SYMBOL VARCHAR, TS BIGINT, BID DOUBLE
) WITH (KAFKA_TOPIC='deduprates', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1, TIMESTAMP='TS');");
        await ctx.ExecuteStatementAsync(@"CREATE STREAM IF NOT EXISTS MSCHED (
  BROKER VARCHAR KEY, SYMBOL VARCHAR, OPEN_TS BIGINT, CLOSE_TS BIGINT
) WITH (KAFKA_TOPIC='msched', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1);");

        // Sunday anchor range: 2020-01-05 (Sun) to 2020-01-12 (Sun)
        var open = Ms("2020-01-05T00:00:00Z");
        var close = Ms("2020-01-12T00:00:00Z");
        await ctx.ExecuteStatementAsync($"INSERT INTO MSCHED (BROKER,SYMBOL,OPEN_TS,CLOSE_TS) VALUES ('B2','S2',{open},{close});");
        // Rates across week
        await ctx.ExecuteStatementAsync($"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B2','S2',{open + 1000}, 101.0);");
        await ctx.ExecuteStatementAsync($"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B2','S2',{open + 3600_000}, 99.0);");

        // Ensure clean slate
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live DELETE TOPIC;");

        var csas = @"CREATE TABLE bar_1wk_live WITH (KAFKA_TOPIC='bar_1wk_live', KEY_FORMAT='JSON', VALUE_FORMAT='JSON') AS
SELECT D.BROKER, D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D JOIN MSCHED S WITHIN 7 DAYS ON (D.BROKER=S.BROKER)
WINDOW TUMBLING (SIZE 7 DAYS)
WHERE D.SYMBOL=S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS
GROUP BY D.BROKER, D.SYMBOL;";
        var created = await ctx.ExecuteStatementAsync(csas);
        Assert.True(created.IsSuccess, created.Message);

        // Wait for materialization and rows to appear (retry)
        async Task AssertRowsEventuallyAsync(string sql, string name)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(180);
            Exception? lastEx = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var cnt = await ctx.QueryStreamCountAsync(sql, TimeSpan.FromSeconds(60));
                    if (cnt > 0) return;
                }
                catch (Exception ex) { lastEx = ex; }
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            throw new TimeoutException($"{name} did not produce rows in time. Last: {lastEx?.Message}");
        }

        await AssertRowsEventuallyAsync("SELECT * FROM bar_1wk_live EMIT CHANGES LIMIT 1;", "bar_1wk_live");

        // cleanup
        await ctx.ExecuteStatementAsync("TERMINATE ALL;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live DELETE TOPIC;");
    }
}
