using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// Generator基底クラス
/// 設計理由：責務分離設計におけるGenerator層の統一実装基盤
/// 強制制約：Builder依存注入必須、文脈解釈と構文組み立ての分離、完全なKSQL文出力責任、エラーハンドリング統一
/// </summary>
internal abstract class GeneratorBase
{
    /// <summary>
    /// 注入されたBuilderインスタンス（読み取り専用）
    /// </summary>
    protected readonly IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> Builders;

    /// <summary>
    /// コンストラクタ（Builder依存注入必須）
    /// </summary>
    protected GeneratorBase(IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> builders)
    {
        Builders = builders ?? throw new ArgumentNullException(nameof(builders));
        ValidateRequiredBuilders();
    }

    /// <summary>
    /// 必須Builderの存在確認
    /// </summary>
    protected virtual void ValidateRequiredBuilders()
    {
        // 基底クラスでは最低限のBuilderのみチェック
        var requiredBuilders = GetRequiredBuilderTypes();

        foreach (var builderType in requiredBuilders)
        {
            if (!Builders.ContainsKey(builderType))
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires {builderType} builder but it was not provided");
            }
        }
    }

    /// <summary>
    /// 派生クラスで必須Builderタイプを定義
    /// </summary>
    protected abstract KsqlBuilderType[] GetRequiredBuilderTypes();

    /// <summary>
    /// Builder取得（型安全）
    /// </summary>
    protected IKsqlBuilder GetBuilder(KsqlBuilderType type)
    {
        if (Builders.TryGetValue(type, out var builder))
        {
            return builder;
        }

        throw new InvalidOperationException(
            $"Builder {type} is not available in {GetType().Name}");
    }

    /// <summary>
    /// Builder存在チェック
    /// </summary>
    protected bool HasBuilder(KsqlBuilderType type)
    {
        return Builders.ContainsKey(type);
    }

    /// <summary>
    /// クエリ組み立て（統一エントリーポイント）
    /// </summary>
    protected static string AssembleQuery(params QueryPart[] parts)
    {
        if (parts == null || parts.Length == 0)
        {
            throw new ArgumentException("Query parts cannot be null or empty");
        }

        // 有効な部品のみフィルタリング
        var validParts = parts
            .Where(p => p != null && p.IsValidOrOptional)
            .OrderBy(p => p.Order)
            .Select(p => p.Content.Trim())
            .Where(content => !string.IsNullOrWhiteSpace(content))
            .ToList();

        if (validParts.Count == 0)
        {
            throw new InvalidOperationException("No valid query parts found");
        }

        var result = string.Join(" ", validParts);

        // 基本的な構文チェック
        ValidateAssembledQuery(result);

        return result;
    }

    /// <summary>
    /// 組み立て済みクエリの基本検証
    /// </summary>
    private static void ValidateAssembledQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new InvalidOperationException("Assembled query is empty");
        }

        // 基本的なKSQL構文チェック
        var upperQuery = query.Trim().ToUpper();

        if (!IsValidKsqlQueryStart(upperQuery))
        {
            throw new InvalidOperationException(
                $"Generated query does not start with valid KSQL command: {query}");
        }

        // バランスチェック（括弧等）
        ValidateQueryBalance(query);
    }

    /// <summary>
    /// 有効なKSQLクエリ開始判定
    /// </summary>
    private static bool IsValidKsqlQueryStart(string upperQuery)
    {
        var validStarts = new[]
        {
            "SELECT", "CREATE STREAM", "CREATE TABLE", "DROP STREAM", "DROP TABLE",
            "INSERT INTO", "SHOW", "DESCRIBE", "EXPLAIN", "LIST", "PRINT"
        };

        return validStarts.Any(start => upperQuery.StartsWith(start));
    }

    /// <summary>
    /// クエリバランス検証（括弧等）
    /// </summary>
    private static void ValidateQueryBalance(string query)
    {
        var parenthesesCount = 0;
        var inString = false;

        for (var i = 0; i < query.Length; i++)
        {
            var ch = query[i];
            switch (ch)
            {
                case '\'':
                    // handle '' escape sequence
                    if (i + 1 < query.Length && query[i + 1] == '\'')
                    {
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inString = !inString;
                    }
                    break;

                case '(':
                    if (!inString) parenthesesCount++;
                    break;

                case ')':
                    if (!inString) parenthesesCount--;
                    break;
            }
        }

        if (parenthesesCount != 0)
        {
            throw new InvalidOperationException(
                $"Unbalanced parentheses in query: {query}");
        }

        if (inString)
        {
            throw new InvalidOperationException(
                $"Unclosed string literal in query: {query}");
        }
    }

    /// <summary>
    /// 構造化クエリ組み立て
    /// </summary>
    protected string AssembleStructuredQuery(QueryStructure structure)
    {
        var result = structure.Validate();
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                $"Invalid query structure: {result.GetErrorMessage()}");
        }

        var parts = new List<QueryPart>();

        // クエリタイプに応じたプレフィックス
        parts.Add(CreateQueryPrefix(structure));

        // 句を順序通りに追加
        foreach (var clause in structure.Clauses.OrderBy(c => GetClauseOrder(c.Type)))
        {
            if (clause.IsValid)
            {
                parts.Add(QueryPart.Required(clause.Content, GetClauseOrder(clause.Type)));
            }
        }

        return AssembleQuery(parts.ToArray());
    }

    /// <summary>
    /// クエリプレフィックス作成
    /// </summary>
    private static QueryPart CreateQueryPrefix(QueryStructure structure)
    {
        return structure.QueryType switch
        {
            "SELECT" => QueryPart.Required("SELECT", 0),
            "CREATE_STREAM_AS" => QueryPart.Required($"CREATE STREAM {structure.TargetObject} AS SELECT", 0),
            "CREATE_TABLE_AS" => QueryPart.Required($"CREATE TABLE {structure.TargetObject} AS SELECT", 0),
            _ => throw new NotSupportedException($"Query type {structure.QueryType} is not supported")
        };
    }

    /// <summary>
    /// 句順序取得
    /// </summary>
    private static int GetClauseOrder(QueryClauseType clauseType)
    {
        return clauseType switch
        {
            QueryClauseType.Select => 10,
            QueryClauseType.From => 20,
            QueryClauseType.Join => 30,
            QueryClauseType.Where => 40,
            QueryClauseType.GroupBy => 50,
            QueryClauseType.Having => 70,
            QueryClauseType.OrderBy => 80,
            QueryClauseType.Limit => 90,
            QueryClauseType.EmitChanges => 100,
            _ => 999
        };
    }

    /// <summary>
    /// C# 型から KSQL 型へのマッピング
    /// </summary>
    protected static string MapToKSqlType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType switch
        {
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(short) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(double) => "DOUBLE",
            Type t when t == typeof(float) => "DOUBLE",
            Type t when t == typeof(decimal) => $"DECIMAL({DecimalPrecisionConfig.DecimalPrecision}, {DecimalPrecisionConfig.DecimalScale})",
            Type t when t == typeof(string) => "VARCHAR",
            Type t when t == typeof(char) => "VARCHAR",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMP",
            Type t when t == typeof(Guid) => "VARCHAR",
            Type t when t == typeof(byte[]) => "BYTES",
            _ when underlyingType.IsEnum => throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported."),
            _ when !underlyingType.IsPrimitive &&
                   underlyingType != typeof(string) &&
                   underlyingType != typeof(char) &&
                   underlyingType != typeof(Guid) &&
                   underlyingType != typeof(byte[]) =>
                throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported."),
            _ => throw new NotSupportedException($"Type '{underlyingType.Name}' is not supported.")
        };
    }

    /// <summary>
    /// エラーハンドリング統一メソッド
    /// </summary>
    protected string HandleGenerationError(string operation, System.Exception exception, string? context = null)
    {
        var errorMessage = $"{GetType().Name} failed during {operation}";

        if (!string.IsNullOrEmpty(context))
        {
            errorMessage += $" (Context: {context})";
        }

        errorMessage += $": {exception.Message}";

        // 開発環境でのデバッグ情報追加
        if (IsDebugMode())
        {
            errorMessage += $"\nStack Trace: {exception.StackTrace}";
        }

        throw new InvalidOperationException(errorMessage, exception);
    }

    /// <summary>
    /// Builder呼び出しの安全ラッパー
    /// </summary>
    protected string SafeCallBuilder(KsqlBuilderType builderType, System.Linq.Expressions.Expression expression, string operation)
    {
        try
        {
            var builder = GetBuilder(builderType);
            return builder.Build(expression);
        }
        catch (Exception ex)
        {
            return HandleGenerationError($"{operation} using {builderType} builder", ex, expression.ToString());
        }
    }

    /// <summary>
    /// 条件付きBuilder呼び出し
    /// </summary>
    protected string? TryCallBuilder(KsqlBuilderType builderType, System.Linq.Expressions.Expression? expression)
    {
        if (expression == null || !HasBuilder(builderType))
        {
            return null;
        }

        try
        {
            var builder = GetBuilder(builderType);
            return builder.Build(expression);
        }
        catch
        {
            // 例外を無視して null を返す（任意処理のため）
            return null;
        }
    }

    /// <summary>
    /// デバッグモード判定
    /// </summary>
    private static bool IsDebugMode()
    {
#if DEBUG
        return true;
#else
        return Environment.GetEnvironmentVariable("KSQL_DEBUG") == "true";
#endif
    }

    /// <summary>
    /// Generator情報取得（デバッグ用）
    /// </summary>
    protected virtual string GetGeneratorInfo()
    {
        var builderTypes = string.Join(", ", Builders.Keys);
        return $"Generator: {GetType().Name}, Available Builders: [{builderTypes}]";
    }

    /// <summary>
    /// 共通後処理（EMIT CHANGES等）
    /// </summary>
    protected string ApplyQueryPostProcessing(string baseQuery, QueryAssemblyContext context)
    {
        var query = baseQuery.Trim();
        var upper = query.ToUpper();

        // Pull Query や TABLE クエリで GROUP BY が指定された場合はエラー
        if (upper.Contains("GROUP BY") && (context.IsPullQuery || context.IsTableQuery))
        {
            throw new InvalidOperationException(
                "GROUP BY is not supported in pull or table queries. Use a push query with EMIT CHANGES instead.");
        }

        // TABLEクエリでは常にPull Query扱いとし、EMIT CHANGESを付与しない
        if (!context.IsTableQuery)
        {
            // GROUP BYを含む場合は常にPush QueryとしてEMIT CHANGESを付与
            if (upper.Contains("GROUP BY") && !upper.Contains("EMIT CHANGES"))
            {
                query += " EMIT CHANGES";
            }
            else if (!context.IsPullQuery && !upper.Contains("EMIT CHANGES"))
            {
                query += " EMIT CHANGES";
            }
        }

        // メタデータに応じた追加処理
        if (context.HasMetadata("WITH_OPTIONS"))
        {
            var withOptions = context.GetMetadata<string>("WITH_OPTIONS");
            if (!string.IsNullOrEmpty(withOptions))
            {
                query += $" WITH ({withOptions})";
            }
        }

        if (!query.TrimEnd().EndsWith(";"))
        {
            query += ";";
        }

        return query;
    }

    /// <summary>
    /// クエリ最適化ヒント適用
    /// </summary>
    protected virtual string ApplyOptimizationHints(string query, QueryAssemblyContext context)
    {
        // 基底クラスでは何もしない（派生クラスで実装）
        return query;
    }

    /// <summary>
    /// ToString実装（デバッグ用）
    /// </summary>
    public override string ToString()
    {
        return GetGeneratorInfo();
    }
}
