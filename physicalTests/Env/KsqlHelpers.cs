using Kafka.Ksql.Linq;
using System;
using System.Net.Http;
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

    /// <summary>
    /// Wait until ksqlDB /info endpoint responds, then optionally wait a grace period.
    /// </summary>
    public static async Task WaitForKsqlReadyAsync(string ksqlBaseUrl, TimeSpan? timeout = null, int graceMs = 0)
    {
        var end = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        using var http = new HttpClient();
        var url = ksqlBaseUrl.TrimEnd('/') + "/info";
        while (DateTime.UtcNow < end)
        {
            try
            {
                using var resp = await http.GetAsync(url);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300)
                {
                    if (graceMs > 0) await Task.Delay(graceMs);
                    return;
                }
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"ksqlDB not ready: {url}");
    }

    /// <summary>
    /// Create KsqlContext (or derived) with retries. The factory is invoked per-attempt.
    /// </summary>
    public static async Task<T> CreateContextWithRetryAsync<T>(Func<T> factory, int retries = 3, int delayMs = 1000) where T : KsqlContext
    {
        Exception? last = null;
        for (var i = 0; i < retries; i++)
        {
            try
            {
                return factory();
            }
            catch (Exception ex)
            {
                last = ex;
            }
            await Task.Delay(delayMs);
        }
        throw last ?? new InvalidOperationException("CreateContextWithRetryAsync failed without exception");
    }
}
