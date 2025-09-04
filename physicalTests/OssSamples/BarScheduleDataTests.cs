using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

public class BarScheduleDataTests
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
        var rates = @"CREATE STREAM IF NOT EXISTS DEDUPRATES (
  BROKER VARCHAR KEY,
  SYMBOL VARCHAR,
  TS BIGINT,
  BID DOUBLE
) WITH (KAFKA_TOPIC='deduprates', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1, TIMESTAMP='TS');";
        var r = await ctx.ExecuteStatementAsync(rates);
        Assert.True(r.IsSuccess, r.Message);

        var sched = @"CREATE STREAM IF NOT EXISTS MSCHED (
  BROKER VARCHAR KEY,
  SYMBOL VARCHAR,
  OPEN_TS BIGINT,
  CLOSE_TS BIGINT
) WITH (KAFKA_TOPIC='msched', KEY_FORMAT='KAFKA', VALUE_FORMAT='JSON', PARTITIONS=1, REPLICAS=1);";
        var s = await ctx.ExecuteStatementAsync(sched);
        Assert.True(s.IsSuccess, s.Message);
    }

    private static long Ms(string isoUtc)
        => (long)(DateTime.Parse(isoUtc, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal)
            - DateTime.UnixEpoch).TotalMilliseconds;

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InsertData_And_ExplainCsas_WithNamingConventions()
    {
        await using var ctx = await CreateReadyContextAsync();
        await EnsureSourcesAsync(ctx);

        // Prepare 2 days window around a Monday anchor (2020-01-06)
        var d0 = Ms("2020-01-06T00:00:00Z");
        var d1 = Ms("2020-01-07T00:00:00Z");
        var wEnd = Ms("2020-01-13T00:00:00Z");

        // Schedule entries (day and week range) for BROKER=B1, SYMBOL=S1
        var insSched1 = $"INSERT INTO MSCHED (BROKER, SYMBOL, OPEN_TS, CLOSE_TS) VALUES ('B1','S1',{d0},{d1});";
        var insSchedW = $"INSERT INTO MSCHED (BROKER, SYMBOL, OPEN_TS, CLOSE_TS) VALUES ('B1','S1',{d0},{wEnd});";
        Assert.True((await ctx.ExecuteStatementAsync(insSched1)).IsSuccess);
        Assert.True((await ctx.ExecuteStatementAsync(insSchedW)).IsSuccess);

        // Rate points across two days
        var r1 = $"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B1','S1',{d0 + 1000},100.0);";
        var r2 = $"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B1','S1',{d0 + 3600_000},110.0);";
        var r3 = $"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B1','S1',{d1 + 2000},120.0);";
        var r4 = $"INSERT INTO DEDUPRATES (BROKER,SYMBOL,TS,BID) VALUES ('B1','S1',{d1 + 3600_000},90.0);";
        Assert.True((await ctx.ExecuteStatementAsync(r1)).IsSuccess);
        Assert.True((await ctx.ExecuteStatementAsync(r2)).IsSuccess);
        Assert.True((await ctx.ExecuteStatementAsync(r3)).IsSuccess);
        Assert.True((await ctx.ExecuteStatementAsync(r4)).IsSuccess);

        // EXPLAIN CSAS using naming convention bar_1d_live and bar_1wk_final
        var csas1d = @"CREATE TABLE bar_1d_live WITH (KAFKA_TOPIC='bar_1d_live', KEY_FORMAT='JSON', VALUE_FORMAT='JSON') AS
SELECT D.BROKER, D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D JOIN MSCHED S WITHIN 1 DAYS ON (D.BROKER=S.BROKER)
WINDOW TUMBLING (SIZE 1 DAY)
WHERE D.SYMBOL=S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS
GROUP BY D.BROKER, D.SYMBOL;";
        // Create materialized table for daily live
        var ct1 = await ctx.ExecuteStatementAsync(csas1d);
        Assert.True(ct1.IsSuccess, ct1.Message);

        var csas1wk = @"CREATE TABLE bar_1wk_final WITH (KAFKA_TOPIC='bar_1wk_final', KEY_FORMAT='JSON', VALUE_FORMAT='JSON') AS
SELECT D.BROKER, D.SYMBOL,
  EARLIEST_BY_OFFSET(D.BID) AS OPEN,
  MAX(D.BID) AS HIGH,
  MIN(D.BID) AS LOW,
  LATEST_BY_OFFSET(D.BID) AS CLOSE
FROM DEDUPRATES D JOIN MSCHED S WITHIN 7 DAYS ON (D.BROKER=S.BROKER)
WINDOW TUMBLING (SIZE 7 DAYS)
WHERE D.SYMBOL=S.SYMBOL AND S.OPEN_TS <= D.TS AND D.TS < S.CLOSE_TS
GROUP BY D.BROKER, D.SYMBOL;";
        var ct2 = await ctx.ExecuteStatementAsync(csas1wk);
        Assert.True(ct2.IsSuccess, ct2.Message);

        // Verify streams registered (short check via SHOW STREAMS)
        var show = await ctx.ExecuteStatementAsync("SHOW TABLES;");
        Assert.True(show.IsSuccess, show.Message);
        var json = show.Message.ToLowerInvariant();
        Assert.Contains("bar_1d_live", json);
        Assert.Contains("bar_1wk_final", json);

        // Count rows via /query-stream using LIMIT to terminate with retry (materialization latency)
        async Task<int> CountEventuallyAsync(string sql)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(120);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var cnt = await ctx.QueryStreamCountAsync(sql, TimeSpan.FromSeconds(45));
                    if (cnt > 0) return cnt;
                }
                catch { }
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
            return 0;
        }

        var c1 = await CountEventuallyAsync("SELECT * FROM bar_1d_live EMIT CHANGES LIMIT 2;");
        var c2 = await CountEventuallyAsync("SELECT * FROM bar_1wk_final EMIT CHANGES LIMIT 2;");
        Assert.True(c1 > 0, $"bar_1d_live returned {c1}");
        Assert.True(c2 > 0, $"bar_1wk_final returned {c2}");

        // Cleanup
        await ctx.ExecuteStatementAsync("TERMINATE ALL;" );
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1d_live DELETE TOPIC;" );
        await ctx.ExecuteStatementAsync("DROP TABLE IF EXISTS bar_1wk_final DELETE TOPIC;" );
    }
}
