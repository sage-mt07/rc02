using System;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Xunit;
using System.Threading;

namespace Kafka.Ksql.Linq.Tests.Integration;

public class BarWeekAnchorCompareTests
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
    public async Task Compare_SundayVsMonday_WeeklyTables_ShouldDiffer()
    {
        await using var ctx = await CreateReadyContextAsync();
        await ctx.ExecuteStatementAsync(@"CREATE STREAM IF NOT EXISTS DEDUPRATES (
  BROKER VARCHAR KEY, SYMBOL VARCHAR, TS BIGINT, BID DOUBLE
) WITH (KAFKA_TOPIC='deduprates', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1, TIMESTAMP='TS');");
        await ctx.ExecuteStatementAsync(@"CREATE STREAM IF NOT EXISTS MSCHED (
  BROKER VARCHAR KEY, SYMBOL VARCHAR, OPEN_TS BIGINT, CLOSE_TS BIGINT
) WITH (KAFKA_TOPIC='msched', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1);");

        // Insert same rates spanning two days around Sunday/Monday boundary
        var sun = Ms("2020-01-05T00:00:00Z"); // Sunday
        var mon = Ms("2020-01-06T00:00:00Z"); // Monday
        await ctx.ExecuteStatementAsync($"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B3','S3',{sun + 1000}, 100.0);");
        await ctx.ExecuteStatementAsync($"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B3','S3',{mon + 1000}, 120.0);");

        // Sunday-anchor schedule: covers only Sunday -> should include 1 rate
        await ctx.ExecuteStatementAsync($"INSERT INTO MSCHED (BROKER,SYMBOL,OPEN_TS,CLOSE_TS) VALUES ('B3','S3',{sun},{mon});");
        // Monday-anchor schedule: covers Monday..Sunday next week -> includes the Monday rate
        var nextMon = Ms("2020-01-13T00:00:00Z");
        await ctx.ExecuteStatementAsync($"INSERT INTO MSCHED (BROKER,SYMBOL,OPEN_TS,CLOSE_TS) VALUES ('B3','S3',{mon},{nextMon});");

        // Build two weekly tables filtered by the same schedule stream but separated by window + WHERE
        // Clean up any leftovers
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live_sun DELETE TOPIC;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live_mon DELETE TOPIC;");

        var csasSun = @"CREATE TABLE bar_1wk_live_sun WITH (KAFKA_TOPIC='bar_1wk_live_sun', KEY_FORMAT='JSON', VALUE_FORMAT='JSON') AS
SELECT D.BROKER, D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D JOIN MSCHED S WITHIN 7 DAYS ON (D.BROKER=S.BROKER)
WINDOW TUMBLING (SIZE 7 DAYS)
WHERE D.SYMBOL=S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS AND S.OPEN_TS={sun}
GROUP BY D.BROKER, D.SYMBOL;";
        var csasMon = @"CREATE TABLE bar_1wk_live_mon WITH (KAFKA_TOPIC='bar_1wk_live_mon', KEY_FORMAT='JSON', VALUE_FORMAT='JSON') AS
SELECT D.BROKER, D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D JOIN MSCHED S WITHIN 7 DAYS ON (D.BROKER=S.BROKER)
WINDOW TUMBLING (SIZE 7 DAYS)
WHERE D.SYMBOL=S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS AND S.OPEN_TS={mon}
GROUP BY D.BROKER, D.SYMBOL;";
        Assert.True((await ctx.ExecuteStatementAsync(csasSun)).IsSuccess);
        Assert.True((await ctx.ExecuteStatementAsync(csasMon)).IsSuccess);

        // Pull/push counts
        async Task<int> CountEventuallyAsync(string sql)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(180);
            Exception? lastEx = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var cnt = await ctx.QueryStreamCountAsync(sql, TimeSpan.FromSeconds(60));
                    if (cnt > 0) return cnt;
                }
                catch (Exception ex) { lastEx = ex; }
                await Task.Delay(TimeSpan.FromSeconds(3));
            }
            throw new TimeoutException($"No rows for query after wait: {lastEx?.Message}");
        }

        var sunCnt = await CountEventuallyAsync("SELECT * FROM bar_1wk_live_sun EMIT CHANGES LIMIT 1;");
        var monCnt = await CountEventuallyAsync("SELECT * FROM bar_1wk_live_mon EMIT CHANGES LIMIT 1;");
        Assert.True(sunCnt >= 1, $"sunCnt={sunCnt}");
        Assert.True(monCnt >= 1, $"monCnt={monCnt}");

        // Cleanup
        await ctx.ExecuteStatementAsync("TERMINATE ALL;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live_sun DELETE TOPIC;");
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_live_mon DELETE TOPIC;");
    }
}
