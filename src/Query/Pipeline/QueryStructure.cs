using System;
using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// クエリ構造定義
/// 設計理由：Generator層でのクエリ構造統一管理
/// </summary>
internal record QueryStructure(
    string QueryType,
    string TargetObject,
    List<QueryClause> Clauses,
    QueryMetadata Metadata)
{
    /// <summary>
    /// SELECT構造作成
    /// </summary>
    public static QueryStructure CreateSelect(string targetObject)
    {
        return new QueryStructure(
            "SELECT",
            targetObject,
            new List<QueryClause>(),
            new QueryMetadata(DateTime.UtcNow, "DML"));
    }

    /// <summary>
    /// CREATE STREAM AS構造作成
    /// </summary>
    public static QueryStructure CreateStreamAs(string streamName, string baseObject)
    {
        return new QueryStructure(
            "CREATE_STREAM_AS",
            streamName,
            new List<QueryClause>(),
            new QueryMetadata(DateTime.UtcNow, "DDL", baseObject));
    }

    /// <summary>
    /// CREATE TABLE AS構造作成
    /// </summary>
    public static QueryStructure CreateTableAs(string tableName, string baseObject)
    {
        return new QueryStructure(
            "CREATE_TABLE_AS",
            tableName,
            new List<QueryClause>(),
            new QueryMetadata(DateTime.UtcNow, "DDL", baseObject));
    }

    /// <summary>
    /// 句追加
    /// </summary>
    private static readonly QueryClauseType[] ClauseInsertionOrder = new[]
    {
        QueryClauseType.Select,
        QueryClauseType.From,
        QueryClauseType.Join,
        QueryClauseType.Where,
        QueryClauseType.GroupBy,
        QueryClauseType.Having,
        QueryClauseType.OrderBy,
        QueryClauseType.Limit,
        QueryClauseType.EmitChanges
    };

    public QueryStructure AddClause(QueryClause clause)
    {
        var newClauses = new List<QueryClause>(Clauses);
        int insertIndex = newClauses.FindIndex(c =>
            Array.IndexOf(ClauseInsertionOrder, clause.Type) <
            Array.IndexOf(ClauseInsertionOrder, c.Type));

        if (insertIndex >= 0)
        {
            newClauses.Insert(insertIndex, clause);
        }
        else
        {
            newClauses.Add(clause);
        }

        return this with { Clauses = newClauses };
    }

    /// <summary>
    /// 複数句追加
    /// </summary>
    public QueryStructure AddClauses(params QueryClause[] clauses)
    {
        var structure = this;
        foreach (var clause in clauses)
        {
            structure = structure.AddClause(clause);
        }
        return structure;
    }

    /// <summary>
    /// 句取得（型別）
    /// </summary>
    public QueryClause? GetClause(QueryClauseType type)
    {
        return Clauses.FirstOrDefault(c => c.Type == type);
    }

    /// <summary>
    /// 句存在チェック
    /// </summary>
    public bool HasClause(QueryClauseType type)
    {
        return Clauses.Any(c => c.Type == type);
    }

    /// <summary>
    /// 句削除
    /// </summary>
    public QueryStructure RemoveClause(QueryClauseType type)
    {
        var newClauses = Clauses.Where(c => c.Type != type).ToList();
        return this with { Clauses = newClauses };
    }

    /// <summary>
    /// メタデータ更新
    /// </summary>
    public QueryStructure WithMetadata(QueryMetadata metadata)
    {
        return this with { Metadata = metadata };
    }

    /// <summary>
    /// 構造検証
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // 基本検証
        if (string.IsNullOrWhiteSpace(TargetObject))
        {
            errors.Add("Target object is required");
        }

        // クエリ型別検証
        switch (QueryType)
        {
            case "SELECT":
                ValidateSelectStructure(errors);
                break;
            case "CREATE_STREAM_AS":
            case "CREATE_TABLE_AS":
                ValidateCreateAsStructure(errors);
                break;
        }

        // 句順序検証
        ValidateClauseOrder(errors);

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// SELECT構造検証
    /// </summary>
    private void ValidateSelectStructure(List<string> errors)
    {
        // SELECTクエリは必須句なし（SELECT *がデフォルト）
        // ただし、GROUP BYがある場合は集約が必要
        if (HasClause(QueryClauseType.GroupBy) && !HasClause(QueryClauseType.Select))
        {
            errors.Add("GROUP BY requires explicit SELECT clause with aggregations");
        }
    }

    /// <summary>
    /// CREATE AS構造検証
    /// </summary>
    private void ValidateCreateAsStructure(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(Metadata.BaseObject))
        {
            errors.Add("CREATE AS requires base object in metadata");
        }

        // CREATE TABLEはGROUP BYまたはウィンドウが推奨
        if (QueryType == "CREATE_TABLE_AS" &&
            !HasClause(QueryClauseType.GroupBy))
        {
            // 警告レベル（エラーではない）
        }
    }

    /// <summary>
    /// 句順序検証
    /// </summary>
    private void ValidateClauseOrder(List<string> errors)
    {
        var expectedOrder = new[]
        {
            QueryClauseType.Select,
            QueryClauseType.From,
            QueryClauseType.Join,
            QueryClauseType.Where,
            QueryClauseType.GroupBy,
            QueryClauseType.Having,
            QueryClauseType.OrderBy,
            QueryClauseType.Limit
        };

        var actualOrder = Clauses.Select(c => c.Type).ToList();
        var lastValidIndex = -1;

        foreach (var clauseType in actualOrder)
        {
            var expectedIndex = Array.IndexOf(expectedOrder, clauseType);
            if (expectedIndex <= lastValidIndex)
            {
                errors.Add($"Clause {clauseType} is in incorrect order");
            }
            lastValidIndex = Math.Max(lastValidIndex, expectedIndex);
        }
    }
}
