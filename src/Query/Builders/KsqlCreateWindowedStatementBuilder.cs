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
    public static string Build(
        string name,
        KsqlQueryModel model,
        string timeframe,
        string? emitOverride = null,
        string? inputOverride = null,
        RenderOptions? options = null,
        string? valueSchemaFullName = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required", nameof(name));
        if (model is null) throw new ArgumentNullException(nameof(model));
        if (string.IsNullOrWhiteSpace(timeframe)) throw new ArgumentException("timeframe required", nameof(timeframe));
        var baseSql = KsqlCreateStatementBuilder.Build(name, model, null, valueSchemaFullName, null, options);
        if (!string.IsNullOrWhiteSpace(emitOverride))
            baseSql = baseSql.Replace("EMIT CHANGES", emitOverride);
        if (!string.IsNullOrWhiteSpace(inputOverride))
        {
            baseSql = OverrideFrom(baseSql, inputOverride);
            // ハブSTREAM入力（*_1s_final_s）の場合、ユーザーが定義した投影メンバー名（AS 別名）を
            // そのまま再集約の引数として参照する（列名の固定は行わない）。
            if (inputOverride.EndsWith("_1s_final_s", StringComparison.OrdinalIgnoreCase))
            {
                // 抽出: FROM ... <alias>
                var fromAliasMatch = System.Text.RegularExpressions.Regex.Match(
                    baseSql,
                    @"\bFROM\s+[A-Za-z_][\w]*\s+([A-Za-z_][\w]*)",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var alias = fromAliasMatch.Success ? fromAliasMatch.Groups[1].Value : "o";

                // 関数引数の読み替え: <FUNC>(...) AS <AliasName> → <FUNC>(alias.AliasName)
                baseSql = System.Text.RegularExpressions.Regex.Replace(
                    baseSql,
                    @"\b(EARLIEST_BY_OFFSET|LATEST_BY_OFFSET|MAX|MIN)\s*\(([^)]*)\)\s+AS\s+([A-Za-z_][A-Za-z0-9_]*)",
                    m =>
                    {
                        var func = m.Groups[1].Value;
                        var member = m.Groups[3].Value; // AS の別名（= DTO メンバー名）
                        var memberRef = $"{alias}.{member.ToUpperInvariant()}";
                        return $"{func}({memberRef}) AS {member}";
                    },
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            }
        }
        var window = FormatWindow(timeframe);
        // Optional GRACE insertion using simple heuristic: if model has AdditionalSettings[graceSeconds] on adapted entity, caller should pre-embed.
        var sql = InjectWindowAfterFrom(baseSql, window);
        return EnsureWindowStartGrouped(sql, timeframe, model.BucketColumnName);
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
            's' => $"WINDOW TUMBLING (SIZE {val} SECONDS)",
            'm' => $"WINDOW TUMBLING (SIZE {val} MINUTES)",
            'h' => $"WINDOW TUMBLING (SIZE {val} HOURS)",
            'd' => $"WINDOW TUMBLING (SIZE {val} DAYS)",
            _ => $"WINDOW TUMBLING (SIZE {val} MINUTES)"
        };
    }

    private static string OverrideFrom(string sql, string source)
    {
        var pattern = new Regex(@"\bFROM\s+([A-Za-z_][\w]*)\s+([A-Za-z_][\w]*)", RegexOptions.IgnoreCase);
        return pattern.Replace(sql, m => $"FROM {source} {m.Groups[2].Value}", 1);
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

    private static string EnsureWindowStartGrouped(string sql, string timeframe, string? bucketColumnName)
    {
        if (string.IsNullOrWhiteSpace(sql)) return sql;
        if (ParseTimeframeSeconds(timeframe) < 60) return sql;
        if (string.IsNullOrWhiteSpace(bucketColumnName))
            throw new InvalidOperationException("WindowStart() must be projected for minute-or-longer tumbling windows.");

        var aliasMatch = new Regex(@"\bFROM\s+[A-Za-z_][\w]*\s+([A-Za-z_][\w]*)", RegexOptions.IgnoreCase)
            .Match(sql);
        var alias = aliasMatch.Success ? aliasMatch.Groups[1].Value : null;
        var bucketUpper = bucketColumnName.ToUpperInvariant();
        var qualified = string.IsNullOrWhiteSpace(alias)
            ? bucketUpper
            : $"{alias}.{bucketUpper}";

        var pattern = new Regex(@"(GROUP\s+BY\s+)([^\n;]+)", RegexOptions.IgnoreCase);
        return pattern.Replace(sql, m =>
        {
            var clause = m.Groups[2].Value;
            if (clause.IndexOf(bucketUpper, StringComparison.OrdinalIgnoreCase) >= 0)
                return m.Value;
            if (!string.Equals(qualified, bucketUpper, StringComparison.OrdinalIgnoreCase) &&
                clause.IndexOf(qualified, StringComparison.OrdinalIgnoreCase) >= 0)
                return m.Value;

            var trimmed = clause.TrimEnd();
            var separator = trimmed.EndsWith(",", StringComparison.Ordinal) ? " " : ", ";
            return m.Groups[1].Value + trimmed + separator + qualified;
        }, 1);
    }

    private static int ParseTimeframeSeconds(string timeframe)
    {
        if (string.IsNullOrWhiteSpace(timeframe)) return 0;
        timeframe = timeframe.Trim();

        if (timeframe.EndsWith("mo", StringComparison.OrdinalIgnoreCase) && int.TryParse(timeframe[..^2], out var months))
            return months * 30 * 24 * 60 * 60;
        if (timeframe.EndsWith("wk", StringComparison.OrdinalIgnoreCase) && int.TryParse(timeframe[..^2], out var weeks))
            return weeks * 7 * 24 * 60 * 60;

        var unit = char.ToLowerInvariant(timeframe[^1]);
        if (!int.TryParse(timeframe[..^1], out var value))
            return 0;

        return unit switch
        {
            's' => value,
            'm' => value * 60,
            'h' => value * 3600,
            'd' => value * 86400,
            _ => 0
        };
    }
}
