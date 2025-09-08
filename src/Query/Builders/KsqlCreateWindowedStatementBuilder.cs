using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Collections.Generic;
using System.Text;
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
            baseSql = InjectWindowStart(baseSql);
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

    internal static string InjectWindowStart(string sql)
    {
        var selectIdx = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        if (selectIdx < 0) return sql;
        var fromIdx = FindFrom(sql, selectIdx + 6);
        var body = sql.Substring(selectIdx + 6, fromIdx - (selectIdx + 6));
        var items = Split(body);
        string? alias = null;
        for (var i = 0; i < items.Count; i++)
        {
            var trimmed = items[i].TrimStart();
            if (trimmed.StartsWith("WINDOWSTART", StringComparison.OrdinalIgnoreCase))
            {
                alias = ExtractAlias(trimmed);
                items.RemoveAt(i);
                break;
            }
        }
        alias ??= "BucketStart";
        items.Insert(0, $"WINDOWSTART AS {alias}");
        var rebuilt = string.Join(", ", items);
        return sql.Substring(0, selectIdx + 6) + " " + rebuilt + " " + sql.Substring(fromIdx);
    }

    private static string ExtractAlias(string item)
    {
        var idx = item.IndexOf("AS", StringComparison.OrdinalIgnoreCase);
        return idx >= 0 ? item[(idx + 2)..].Trim() : "BucketStart";
    }

    private static int FindFrom(string sql, int start)
    {
        var depth = 0; var single = false; var dbl = false;
        for (var i = start; i < sql.Length - 3; i++)
        {
            var c = sql[i];
            if (c == '\'' && !dbl) single = !single;
            else if (c == '"' && !single) dbl = !dbl;
            else if (!single && !dbl)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;
                else if (depth == 0 && (c == 'F' || c == 'f') &&
                         sql.AsSpan(i, 4).Equals("from", StringComparison.OrdinalIgnoreCase))
                {
                    if (i == 0 || char.IsWhiteSpace(sql[i - 1])) return i;
                }
            }
        }
        return sql.Length;
    }

    private static List<string> Split(string body)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        var depth = 0; var single = false; var dbl = false;
        foreach (var c in body)
        {
            if (c == ',' && depth == 0 && !single && !dbl)
            {
                list.Add(sb.ToString().Trim());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
                if (c == '\'' && !dbl) single = !single;
                else if (c == '"' && !single) dbl = !dbl;
                else if (!single && !dbl)
                {
                    if (c == '(') depth++;
                    else if (c == ')') depth--;
                }
            }
        }
        var last = sb.ToString().Trim();
        if (last.Length > 0) list.Add(last);
        return list;
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
        // Replace first occurrence of "FROM <ident> [alias]" with "FROM <ident> [alias] {window}"
        var pattern = new Regex(@"\bFROM\s+([A-Za-z_][\w]*)(\s+[A-Za-z_][\w]*)?", RegexOptions.IgnoreCase);
        return pattern.Replace(sql, m =>
        {
            var alias = m.Groups[2].Value;
            return $"FROM {m.Groups[1].Value}{alias} {windowClause}";
        }, 1);
    }
}
