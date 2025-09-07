using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Kafka.Ksql.Linq.Query.Builders;

/// <summary>
/// Builds CREATE STREAM/TABLE AS statements that include WINDOW TUMBLING clause
/// by adapting output from KsqlCreateStatementBuilder and injecting window spec.
/// </summary>
internal static class KsqlCreateWindowedStatementBuilder
{
    public static string Build(string name, KsqlQueryModel model, string timeframe)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(timeframe)) throw new ArgumentException("timeframe required", nameof(timeframe));
        var baseSql = KsqlCreateStatementBuilder.Build(name, model);
        if (model.IsFinal)
        {
            // remove any existing BucketStart alias from the SELECT list; regex scope is limited to SELECT
            // TODO: anchor to the SELECT segment explicitly if the builder ever rewrites other clauses
            baseSql = Regex.Replace(baseSql, @",\s*[^,]*?AS BucketStart", string.Empty, RegexOptions.IgnoreCase);
            baseSql = baseSql.Replace("SELECT ", "SELECT WINDOWSTART AS BucketStart, ");
        }
        var window = FormatWindow(model, timeframe);
        var sql = InjectWindowAfterFrom(baseSql, window);
        sql = InjectEmitMode(sql, model);
        return sql;
    }

    public static Dictionary<string, string> BuildAll(string namePrefix, KsqlQueryModel model, Func<string, string> nameFormatter)
    {
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (nameFormatter is null) throw new ArgumentNullException(nameof(nameFormatter));
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tf in model.Windows)
        {
            var name = nameFormatter(tf);
            result[tf] = Build(name, model, tf);
        }
        return result;
    }

    private static string FormatWindow(KsqlQueryModel model, string timeframe)
    {
        // timeframe like: 1m, 5m, 1h, 1d, 7d, 1wk, 1mo
        var grace = model.IsFinal && model.GraceSeconds.HasValue && model.GraceSeconds.Value > 0
            ? $", GRACE PERIOD {FormatDuration(model.GraceSeconds.Value)}"
            : string.Empty;
        if (timeframe.EndsWith("wk", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(timeframe[..^2], out var w))
                return $"WINDOW TUMBLING (SIZE {w * 7} DAYS{grace})";
        }
        if (timeframe.EndsWith("mo", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(timeframe[..^2], out var mo))
                return $"WINDOW TUMBLING (SIZE {mo} MONTHS{grace})"; // KSQL supports MONTHS in recent versions
        }
        var unit = timeframe[^1];
        if (!int.TryParse(timeframe[..^1], out var val)) val = 1;
        return unit switch
        {
            'm' => $"WINDOW TUMBLING (SIZE {val} MINUTES{grace})",
            'h' => $"WINDOW TUMBLING (SIZE {val} HOURS{grace})",
            'd' => $"WINDOW TUMBLING (SIZE {val} DAYS{grace})",
            _ => $"WINDOW TUMBLING (SIZE {val} MINUTES{grace})"
        };
    }

    private static string InjectEmitMode(string sql, KsqlQueryModel model)
    {
        // KsqlCreateStatementBuilder already appends EMIT CHANGES for push mode by default.
        // Override to EMIT FINAL when model.IsFinal is true.
        if (model.IsFinal)
        {
            sql = sql.Replace("EMIT CHANGES", "EMIT FINAL");
        }
        return sql;
    }

    private static string FormatDuration(int seconds)
    {
        if (seconds % 86400 == 0) return $"{seconds / 86400} DAYS";
        if (seconds % 3600 == 0) return $"{seconds / 3600} HOURS";
        if (seconds % 60 == 0) return $"{seconds / 60} MINUTES";
        return $"{seconds} SECONDS";
    }

    private static string InjectWindowAfterFrom(string sql, string windowClause)
    {
        // naive injection: replace first occurrence of "FROM <ident>" with "FROM <ident> {window}"
        var pattern = new Regex(@"\bFROM\s+([A-Za-z_][\w]*)", RegexOptions.IgnoreCase);
        return pattern.Replace(sql, m => $"FROM {m.Groups[1].Value} {windowClause}", 1);
    }
}
