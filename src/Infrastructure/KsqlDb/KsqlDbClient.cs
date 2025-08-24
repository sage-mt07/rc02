using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;

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

    public void Dispose()
    {
        _client.Dispose();
    }
}
