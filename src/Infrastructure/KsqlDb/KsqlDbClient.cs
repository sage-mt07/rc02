using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using System.IO;
using System.Threading;

namespace Kafka.Ksql.Linq.Infrastructure.KsqlDb;

internal class KsqlDbClient : IKsqlDbClient, IDisposable
{
    private readonly HttpClient _client;

    public KsqlDbClient(Uri baseAddress)
    {
        _client = new HttpClient { BaseAddress = baseAddress };
    }

    public async Task<KsqlDbResponse> ExecuteStatementAsync(string statement)
    {
        var payload = new { ksql = statement, streamsProperties = new { } };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/ksql", content);
        var body = await response.Content.ReadAsStringAsync();
        var success = response.IsSuccessStatusCode && !body.Contains("\"error_code\"");
        return new KsqlDbResponse(success, body);
    }

    public Task<KsqlDbResponse> ExecuteExplainAsync(string ksql)
    {
        return ExecuteStatementAsync($"EXPLAIN {ksql}");
    }

    public async Task<HashSet<string>> GetTableTopicsAsync()
    {
        var sql = "SHOW TABLES;";
        var response = await ExecuteStatementAsync(sql);
        var tableTopics = new HashSet<string>();
        if (!response.IsSuccess)
            return tableTopics;

        try
        {
            using var doc = JsonDocument.Parse(response.Message);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                if (item.TryGetProperty("tables", out var arr))
                {
                    foreach (var element in arr.EnumerateArray())
                    {
                        // ここを "topic" に修正
                        if (element.TryGetProperty("topic", out var topicEl) && topicEl.ValueKind == JsonValueKind.String)
                        {
                            var topic = topicEl.GetString();
                            if (!string.IsNullOrEmpty(topic))
                                tableTopics.Add(topic.ToLowerInvariant()); // 小文字化が安全
                        }
                        // 念のため "name"（テーブル名）も突合せ用に追加しておくと安心
                        if (element.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                        {
                            var tableName = nameEl.GetString();
                            if (!string.IsNullOrEmpty(tableName))
                                tableTopics.Add(tableName.ToLowerInvariant());
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        return tableTopics;
    }

    public async Task<int> ExecuteQueryStreamCountAsync(string sql, TimeSpan? timeout = null)
    {
        // Include streamsProperties to ensure we consume from the beginning in tests
        var payload = new
        {
            sql,
            streamsProperties = new System.Collections.Generic.Dictionary<string, object>
            {
                ["auto.offset.reset"] = "earliest"
            }
        };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/query-stream") { Content = content };
        // Stream the response without buffering the whole content
        using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        int count = 0;
        while (!reader.EndOfStream && !cts.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;
            // Heuristic: ksqlDB returns JSON lines with a "row" field for data
            if (line.IndexOf("\"row\"", StringComparison.OrdinalIgnoreCase) >= 0)
                count++;
        }
        return count;
    }

    public async Task<int> ExecutePullQueryCountAsync(string sql, TimeSpan? timeout = null)
    {
        var payload = new { sql };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var cts = timeout.HasValue ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var response = await _client.PostAsync("/query", content, cts.Token);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cts.Token);
        try
        {
            using var doc = JsonDocument.Parse(body);
            int cnt = 0;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.Object && el.TryGetProperty("row", out _))
                        cnt++;
                }
            }
            return cnt;
        }
        catch
        {
            // Fallback: count occurrences of "row"
            var idx = 0; int count = 0;
            while ((idx = body.IndexOf("\"row\"", idx, StringComparison.OrdinalIgnoreCase)) >= 0)
            { count++; idx += 5; }
            return count;
        }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
