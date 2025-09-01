namespace Kafka.Ksql.Linq.Infrastructure.KsqlDb;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

internal class DictionaryKvClient : IDictionaryKvClient
{
    private readonly IKsqlDbClient _client;
    private readonly string _table;

    public DictionaryKvClient(IKsqlDbClient client, string table)
    {
        _client = client;
        _table = table;
    }

    public async Task<string?> GetAsync(string key)
    {
        var stmt = $"SELECT v FROM {_table} WHERE k='{key}';";
        var res = await _client.ExecuteStatementAsync(stmt);
        if (!res.IsSuccess)
            throw new System.InvalidOperationException($"Dictionary lookup failed for '{key}': {res.Message}");
        var values = new System.Collections.Generic.List<string?>();
        try
        {
            using var doc = JsonDocument.Parse(res.Message);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.TryGetProperty("row", out var row))
                {
                    var cols = row.GetProperty("columns");
                    values.Add(cols[0].GetString());
                }
            }
        }
        catch { }
        if (values.Count == 0)
            return null;
        if (values.Count > 1)
            throw new System.InvalidOperationException($"Dictionary contains duplicate entries for '{key}'.");
        var value = values[0];
        if (string.IsNullOrWhiteSpace(value))
            throw new System.InvalidOperationException($"Dictionary entry for '{key}' is empty.");
        return value;
    }

    public async Task<Dictionary<string, string>> GetByPrefixAsync(string prefix)
    {
        var stmt = $"SELECT k, v FROM {_table} WHERE k LIKE '{prefix}%';";
        var res = await _client.ExecuteStatementAsync(stmt);
        if (!res.IsSuccess)
            throw new System.InvalidOperationException($"Dictionary prefix lookup failed for '{prefix}': {res.Message}");
        var dict = new Dictionary<string, string>();
        try
        {
            using var doc = JsonDocument.Parse(res.Message);
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.TryGetProperty("row", out var row))
                {
                    var cols = row.GetProperty("columns");
                    var k = cols[0].GetString();
                    var v = cols[1].GetString() ?? string.Empty;
                    if (k != null)
                        dict[k] = v;
                }
            }
        }
        catch { }
        return dict;
    }

    public Task UpsertAsync(string key, string value)
    {
        var stmt = $"UPSERT INTO {_table} VALUES('{key}','{value}');";
        return _client.ExecuteStatementAsync(stmt);
    }
}
