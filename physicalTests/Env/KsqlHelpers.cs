using Kafka.Ksql.Linq;
using System;
using System.Threading.Tasks;

namespace PhysicalTestEnv;

public static class KsqlHelpers
{
    public static async Task<KsqlDbResponse> ExecuteStatementWithRetryAsync(KsqlContext ctx, string statement, int retries = 3, int delayMs = 1000)
    {
        Exception? last = null;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                var r = await ctx.ExecuteStatementAsync(statement);
                if (r.IsSuccess) return r;
                last = new InvalidOperationException(r.Message);
            }
            catch (Exception ex)
            {
                last = ex;
            }
            await Task.Delay(delayMs);
        }
        throw last ?? new InvalidOperationException("ExecuteStatementWithRetryAsync failed without exception");
    }
}

