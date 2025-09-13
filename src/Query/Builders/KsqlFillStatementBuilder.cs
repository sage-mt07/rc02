using System;
using System.Linq;
using System.Text;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// Builds CREATE TABLE AS SELECT for Fill role.
/// Drives from Heartbeat (HB) buckets and left-joins the live table to ensure
/// contiguous buckets are materialized. Value columns fall back to NULL when live is missing.
/// A subsequent revision may incorporate prev_1m to project previous-close-based fillers.
/// </summary>
internal static class KsqlFillStatementBuilder
{
    public static string Build(
        string name,
        string[] keys,
        string[] projection,
        string bucketColumn,
        string hbTable,
        string liveTable)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        if (keys == null) keys = Array.Empty<string>();
        if (projection == null) projection = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(bucketColumn)) bucketColumn = "BucketStart";
        if (string.IsNullOrWhiteSpace(hbTable)) throw new ArgumentException("hbTable required", nameof(hbTable));
        if (string.IsNullOrWhiteSpace(liveTable)) throw new ArgumentException("liveTable required", nameof(liveTable));

        var sb = new StringBuilder();
        sb.Append($"CREATE TABLE {name} WITH (KAFKA_TOPIC='{name}', KEY_FORMAT='AVRO', VALUE_FORMAT='AVRO') AS\n");

        // SELECT clause: project keys, bucket, and value columns from live when present.
        var selectParts = new System.Collections.Generic.List<string>();
        foreach (var k in keys)
            selectParts.Add($"h.KEY->{k.ToUpperInvariant()} AS {k}");
        selectParts.Add($"h.{bucketColumn} AS {bucketColumn}");
        foreach (var col in projection)
        {
            if (keys.Contains(col, StringComparer.OrdinalIgnoreCase)) continue;
            if (string.Equals(col, bucketColumn, StringComparison.OrdinalIgnoreCase)) continue;
            selectParts.Add($"l.{col} AS {col}");
        }
        sb.Append("SELECT ").Append(string.Join(", ", selectParts)).AppendLine();

        // FROM + JOIN on all keys and the bucket column.
        sb.Append($"FROM {hbTable} h LEFT JOIN {liveTable} l ON ");
        var onParts = keys.Select(k => $"h.KEY->{k.ToUpperInvariant()} = l.KEY->{k.ToUpperInvariant()}").ToList();
        onParts.Add($"h.{bucketColumn} = l.{bucketColumn}");
        sb.Append(string.Join(" AND ", onParts)).AppendLine();

        // Emit changes by default for Fill role.
        sb.Append("EMIT CHANGES;");
        return sb.ToString();
    }
}

