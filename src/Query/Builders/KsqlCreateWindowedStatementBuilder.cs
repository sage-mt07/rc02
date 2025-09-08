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
        var window = FormatWindow(timeframe);
        var sql = InjectWindowAfterFrom(baseSql, window);
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

    private static string FormatWindow(string timeframe)
    {
        // timeframe like: 1m, 5m, 1h, 1d, 7d, 1wk, 1mo
        if (timeframe.EndsWith("wk", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(timeframe[..^2], out var w))
                return $"WINDOW TUMBLING (SIZE {w * 7} DAYS)";
        }
        if (timeframe.EndsWith("mo", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(timeframe[..^2], out var mo))
                return $"WINDOW TUMBLING (SIZE {mo} MONTHS)"; // KSQL supports MONTHS in recent versions
        }
        var unit = timeframe[^1];
        if (!int.TryParse(timeframe[..^1], out var val)) val = 1;
        return unit switch
        {
            'm' => $"WINDOW TUMBLING (SIZE {val} MINUTES)",
            'h' => $"WINDOW TUMBLING (SIZE {val} HOURS)",
            'd' => $"WINDOW TUMBLING (SIZE {val} DAYS)",
            _ => $"WINDOW TUMBLING (SIZE {val} MINUTES)"
        };
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
