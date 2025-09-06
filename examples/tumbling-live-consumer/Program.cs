using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var ksqlUrl = Environment.GetEnvironmentVariable("KSQL_URL") ?? "http://localhost:8088";
        var minutes = 1; // 1m tumbling
        var durationSec = 60; // run ~60s

        foreach (var a in args)
        {
            if (a.StartsWith("--minutes=")) int.TryParse(a.Substring("--minutes=".Length), out minutes);
            if (a.StartsWith("--duration=")) int.TryParse(a.Substring("--duration=".Length), out durationSec);
        }

        var unit = minutes == 1 ? "MINUTE" : "MINUTES";
        var sql = "SELECT " +
                  " dedupraterecord.key->Broker AS BROKER, " +
                  " dedupraterecord.key->Symbol AS SYMBOL, " +
                  " WINDOWSTART AS WS, WINDOWEND AS WE, " +
                  " EARLIEST_BY_OFFSET(Bid) AS OPEN, MAX(Bid) AS HIGH, MIN(Bid) AS LOW, LATEST_BY_OFFSET(Bid) AS CLOSE " +
                  " FROM DEDUPRATES WINDOW TUMBLING (SIZE " + minutes + " " + unit + ") " +
                  " GROUP BY dedupraterecord.key->Broker, dedupraterecord.key->Symbol " +
                  " EMIT CHANGES;";

        using var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSec));

        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(ksqlUrl), "/query-stream"));
        req.Headers.Add("Accept", "application/vnd.ksql.v1+json");
        req.Content = new StringContent(JsonSerializer.Serialize(new { sql }), Encoding.UTF8, "application/vnd.ksql.v1+json");

        Console.WriteLine($"[consuming] {minutes}m tumbling for ~{durationSec}s @ {ksqlUrl}");
        try
        {
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
            using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
            while (!reader.EndOfStream && !cts.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                Console.WriteLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[consuming] canceled by duration");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[consuming] error: {ex.Message}");
            return 1;
        }
        return 0;
    }
}
