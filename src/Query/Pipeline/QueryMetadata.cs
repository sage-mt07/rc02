using System;
using System.Collections.Generic;

namespace Kafka.Ksql.Linq.Query.Pipeline;

internal record QueryMetadata(
    DateTime CreatedAt,
    string Category,
    string? BaseObject = null,
    Dictionary<string, object>? Properties = null)
{
    /// <summary>
    /// プロパティ追加
    /// </summary>
    public QueryMetadata WithProperty(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(Properties ?? new Dictionary<string, object>())
        {
            [key] = value
        };
        return this with { Properties = newProperties };
    }

    /// <summary>
    /// プロパティ取得
    /// </summary>
    public T? GetProperty<T>(string key)
    {
        if (Properties?.TryGetValue(key, out var value) == true && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }
}

