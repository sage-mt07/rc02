using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Common;
using Kafka.Ksql.Linq.Core.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// DMLクエリ生成器（新Builder使用版）
/// 設計理由：責務分離設計に準拠、Builder統合型でSELECT文生成
/// </summary>
internal class DMLQueryGenerator : GeneratorBase, IDMLQueryGenerator
{
    /// <summary>
    /// コンストラクタ（Builder依存注入）
    /// </summary>
    public DMLQueryGenerator(IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> builders)
        : base(builders)
    {
    }

    /// <summary>
    /// 簡易コンストラクタ（標準Builder使用）
    /// </summary>
    public DMLQueryGenerator() : this(CreateStandardBuilders())
    {
    }

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return new[]
        {
            KsqlBuilderType.Where,
            KsqlBuilderType.Select
        };
    }

    /// <summary>
    /// SELECT * クエリ生成
    /// </summary>
    public string GenerateSelectAll(string objectName, bool isPullQuery = true, bool isTableQuery = false)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(objectName, isPullQuery, isTableQuery);
            var structure = CreateSelectStructure(objectName);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("SELECT ALL generation", ex, $"Object: {objectName}");
        }
    }

    /// <summary>
    /// 条件付きSELECTクエリ生成
    /// </summary>
    public string GenerateSelectWithCondition(string objectName, Expression whereExpression, bool isPullQuery = true, bool isTableQuery = false)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(objectName, isPullQuery, isTableQuery);
            var structure = CreateSelectStructure(objectName);

            // WHERE句追加
            var whereContent = SafeCallBuilder(KsqlBuilderType.Where, whereExpression, "WHERE condition processing");
            var whereClause = QueryClause.Required(QueryClauseType.Where, $"WHERE {whereContent}", whereExpression);
            structure = structure.AddClause(whereClause);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("SELECT with condition generation", ex, $"Object: {objectName}");
        }
    }

    /// <summary>
    /// COUNTクエリ生成
    /// </summary>
    public string GenerateCountQuery(string objectName)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(objectName, true); // Pull Query
            var structure = CreateCountStructure(objectName);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context); // COUNTクエリも後処理でセミコロン等を付与
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("COUNT query generation", ex, $"Object: {objectName}");
        }
    }

    /// <summary>
    /// 集約クエリ生成
    /// </summary>
    public string GenerateAggregateQuery(string objectName, Expression aggregateExpression)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(objectName, true); // Aggregates default to pull query
            var structure = CreateSelectStructure(objectName);

            // 集約式処理
            var selectContent = SafeCallBuilder(KsqlBuilderType.Select, aggregateExpression, "aggregate expression processing");
            var selectClause = QueryClause.Required(QueryClauseType.Select, selectContent, aggregateExpression);

            // デフォルトのSELECT *を置き換え
            structure = structure.RemoveClause(QueryClauseType.Select);
            structure = structure.AddClause(selectClause);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("aggregate query generation", ex, $"Object: {objectName}");
        }
    }

    /// <summary>
    /// 複雑なLINQクエリ生成
    /// </summary>
    public string GenerateLinqQuery(string objectName, Expression linqExpression, bool isPullQuery = false, bool isTableQuery = false)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(objectName, isPullQuery, isTableQuery);
            var structure = CreateSelectStructure(objectName);

            // LINQ式を解析してクエリ句を構築
            structure = ProcessLinqExpression(structure, linqExpression, context);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("LINQ query generation", ex, $"Object: {objectName}");
        }
    }

    /// <summary>
    /// 基本SELECT構造作成
    /// </summary>
    private static QueryStructure CreateSelectStructure(string objectName)
    {
        var metadata = new QueryMetadata(DateTime.UtcNow, "DML");
        var structure = QueryStructure.CreateSelect(objectName).WithMetadata(metadata);

        // デフォルトのSELECT *句とFROM句を追加
        var selectClause = QueryClause.Required(QueryClauseType.Select, "*");
        var fromClause = QueryClause.Required(QueryClauseType.From, $"FROM {objectName}");
        return structure.AddClauses(selectClause, fromClause);
    }

    /// <summary>
    /// COUNT構造作成
    /// </summary>
    private static QueryStructure CreateCountStructure(string objectName)
    {
        var metadata = new QueryMetadata(DateTime.UtcNow, "DML");
        var structure = QueryStructure.CreateSelect(objectName).WithMetadata(metadata);

        // COUNT(*)句とFROM句を追加
        var selectClause = QueryClause.Required(QueryClauseType.Select, "COUNT(*)");
        var fromClause = QueryClause.Required(QueryClauseType.From, $"FROM {objectName}");
        return structure.AddClauses(selectClause, fromClause);
    }

    /// <summary>
    /// LINQ式処理
    /// </summary>
    private QueryStructure ProcessLinqExpression(QueryStructure structure, Expression linqExpression, QueryAssemblyContext context)
    {
        var analysis = AnalyzeLinqExpression(linqExpression);
        structure = structure.WithMetadata(analysis.ToMetadata());

        foreach (var methodCall in analysis.MethodCalls.AsEnumerable().Reverse())
        {
            structure = ProcessMethodCall(structure, methodCall, context);
        }

        return structure;
    }

    /// <summary>
    /// メソッド呼び出し処理
    /// </summary>
    private QueryStructure ProcessMethodCall(QueryStructure structure, MethodCallExpression methodCall, QueryAssemblyContext context)
    {
        var methodName = methodCall.Method.Name;

        return methodName switch
        {
            "Select" => ProcessSelectMethod(structure, methodCall),
            "Where" => ProcessWhereMethod(structure, methodCall),
            "GroupBy" => ProcessGroupByMethod(structure, methodCall),
            "Having" => ProcessHavingMethod(structure, methodCall),
            "Join" => ProcessJoinMethod(structure, methodCall),
            "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" => ProcessOrderByMethod(structure, methodCall),
            "Take" => ProcessTakeMethod(structure, methodCall),
            "Skip" => ProcessSkipMethod(structure, methodCall),
            _ => structure // 未対応メソッドは無視
        };
    }

    /// <summary>
    /// SELECT メソッド処理
    /// </summary>
    private QueryStructure ProcessSelectMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (methodCall.Arguments.Count >= 2)
        {
            var lambdaBody = ExtractLambdaBody(methodCall.Arguments[1]);
            if (lambdaBody != null)
            {
                var selectContent = SafeCallBuilder(KsqlBuilderType.Select, lambdaBody, "SELECT processing");
                var clause = QueryClause.Required(QueryClauseType.Select, selectContent, lambdaBody);

                // 既存のSELECT句を置き換え
                structure = structure.RemoveClause(QueryClauseType.Select);
                structure = structure.AddClause(clause);
            }
        }

        return structure;
    }

    /// <summary>
    /// WHERE メソッド処理
    /// </summary>
    private QueryStructure ProcessWhereMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (methodCall.Arguments.Count >= 2)
        {
            var lambdaBody = ExtractLambdaBody(methodCall.Arguments[1]);
            if (lambdaBody != null)
            {
                bool treatAsHaving =
                    structure.HasClause(QueryClauseType.GroupBy) &&
                    HasAggregateFunction(lambdaBody);

                if (treatAsHaving)
                {
                    var havingContent = SafeCallBuilder(KsqlBuilderType.Having, lambdaBody, "HAVING processing");
                    var existingHaving = structure.GetClause(QueryClauseType.Having);
                    if (existingHaving != null)
                    {
                        var combinedContent = $"HAVING ({existingHaving.Content.Substring(7)}) AND ({havingContent})";
                        var combinedClause = QueryClause.Required(QueryClauseType.Having, combinedContent, lambdaBody);
                        structure = structure.RemoveClause(QueryClauseType.Having);
                        structure = structure.AddClause(combinedClause);
                    }
                    else
                    {
                        var havingClause = QueryClause.Required(QueryClauseType.Having, $"HAVING {havingContent}", lambdaBody);
                        structure = structure.AddClause(havingClause);
                    }
                }
                else
                {
                    var whereContent = SafeCallBuilder(KsqlBuilderType.Where, lambdaBody, "WHERE processing");

                    // 既存のWHERE句と結合（AND条件）
                    var existingWhere = structure.GetClause(QueryClauseType.Where);
                    if (existingWhere != null)
                    {
                        var combinedContent = $"WHERE ({existingWhere.Content.Substring(6)}) AND ({whereContent})";
                        var combinedClause = QueryClause.Required(QueryClauseType.Where, combinedContent, lambdaBody);
                        structure = structure.RemoveClause(QueryClauseType.Where);
                        structure = structure.AddClause(combinedClause);
                    }
                    else
                    {
                        var whereClause = QueryClause.Required(QueryClauseType.Where, $"WHERE {whereContent}", lambdaBody);
                        structure = structure.AddClause(whereClause);
                    }
                }
            }
        }

        return structure;
    }

    /// <summary>
    /// GROUP BY メソッド処理
    /// </summary>
    private QueryStructure ProcessGroupByMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (methodCall.Arguments.Count >= 2)
        {
            var lambdaBody = ExtractLambdaBody(methodCall.Arguments[1]);
            if (lambdaBody != null)
            {
                var groupByContent = SafeCallBuilder(KsqlBuilderType.GroupBy, lambdaBody, "GROUP BY processing");
                var clause = QueryClause.Required(QueryClauseType.GroupBy, $"GROUP BY {groupByContent}", lambdaBody);
                structure = structure.AddClause(clause);
            }
        }

        return structure;
    }

    /// <summary>
    /// HAVING メソッド処理
    /// </summary>
    private QueryStructure ProcessHavingMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (HasBuilder(KsqlBuilderType.Having) && methodCall.Arguments.Count >= 2)
        {
            var lambdaBody = ExtractLambdaBody(methodCall.Arguments[1]);
            if (lambdaBody != null)
            {
                var havingContent = SafeCallBuilder(KsqlBuilderType.Having, lambdaBody, "HAVING processing");
                var clause = QueryClause.Required(QueryClauseType.Having, $"HAVING {havingContent}", lambdaBody);
                structure = structure.AddClause(clause);
            }
        }

        return structure;
    }


    /// <summary>
    /// ORDER BY メソッド処理
    /// </summary>
    private QueryStructure ProcessOrderByMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (HasBuilder(KsqlBuilderType.OrderBy))
        {
            var orderByContent = SafeCallBuilder(KsqlBuilderType.OrderBy, methodCall, "ORDER BY processing");
            var clause = QueryClause.Optional(QueryClauseType.OrderBy, $"ORDER BY {orderByContent}", methodCall);

            // ORDER BY は1つの句に統合するため既存句を置き換え
            structure = structure.RemoveClause(QueryClauseType.OrderBy);
            structure = structure.AddClause(clause);
        }

        return structure;
    }

    /// <summary>
    /// TAKE メソッド処理（LIMIT句）
    /// </summary>
    private QueryStructure ProcessTakeMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (methodCall.Arguments.Count >= 2)
        {
            var limitValue = ExtractConstantValue(methodCall.Arguments[1]);
            var clause = QueryClause.Optional(QueryClauseType.Limit, $"LIMIT {limitValue}", methodCall);
            structure = structure.AddClause(clause);
        }

        return structure;
    }

    /// <summary>
    /// SKIP メソッド処理（KSQL未対応のため警告）
    /// </summary>
    private QueryStructure ProcessSkipMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        Console.WriteLine("[KSQL-LINQ WARNING] SKIP/OFFSET is not supported in KSQL. Use WHERE conditions for filtering instead.");
        return structure;
    }

    /// <summary>
    /// JOIN メソッド処理 (単純内部JOINのみ対応)
    /// </summary>
    private QueryStructure ProcessJoinMethod(QueryStructure structure, MethodCallExpression methodCall)
    {
        if (HasBuilder(KsqlBuilderType.Join))
        {
            var joinContent = SafeCallBuilder(KsqlBuilderType.Join, methodCall, "JOIN processing");
            var fromIndex = joinContent.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            if (fromIndex >= 0)
            {
                var fromPart = joinContent.Substring(fromIndex); // FROM table JOIN ...
                var clause = QueryClause.Required(QueryClauseType.From, fromPart, methodCall);
                structure = structure.RemoveClause(QueryClauseType.From);
                structure = structure.AddClause(clause);
            }
        }

        return structure;
    }

    /// <summary>
    /// LINQ式解析
    /// </summary>
    private ExpressionAnalysisResult AnalyzeLinqExpression(Expression expression)
    {
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(expression);
        var result = visitor.Result;
        if (result.Windows.Count > 0)
        {
            if (result.TimeKey == null)
                throw new InvalidOperationException("Time key is required");
            if (!result.GroupByKeys.Contains(result.TimeKey) && !result.GroupByKeys.Contains("BucketStart"))
                throw new InvalidOperationException("Time key must be part of GroupBy keys");
        }
        return result;
    }

    /// <summary>
    /// Lambda Body抽出
    /// </summary>
    private static Expression? ExtractLambdaBody(Expression expression)
    {
        return BuilderValidation.ExtractLambdaBody(expression);
    }

    /// <summary>
    /// 定数値抽出
    /// </summary>
    private static string ExtractConstantValue(Expression expression)
    {
        return expression switch
        {
            ConstantExpression constant => constant.Value?.ToString() ?? "0",
            UnaryExpression unary => ExtractConstantValue(unary.Operand),
            _ => "0"
        };
    }

    /// <summary>
    /// 式に集約関数が含まれるか判定
    /// </summary>
    private static bool HasAggregateFunction(Expression expression)
    {
        var visitor = new AggregateDetectionVisitor();
        visitor.Visit(expression);
        return visitor.HasAggregates;
    }

    /// <summary>
    /// 標準Builder作成
    /// </summary>
    private static IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> CreateStandardBuilders()
    {
        return new Dictionary<KsqlBuilderType, IKsqlBuilder>
        {
            [KsqlBuilderType.Select] = new SelectClauseBuilder(),
            [KsqlBuilderType.Where] = new WhereClauseBuilder(),
            [KsqlBuilderType.GroupBy] = new GroupByClauseBuilder(),
            [KsqlBuilderType.Having] = new HavingClauseBuilder(),
            [KsqlBuilderType.Join] = new JoinClauseBuilder(),
            [KsqlBuilderType.OrderBy] = new OrderByClauseBuilder()
        };
    }

    /// <summary>
    /// 最適化ヒント適用
    /// </summary>
    protected override string ApplyOptimizationHints(string query, QueryAssemblyContext context)
    {
        var optimizedQuery = query;

        // Pull Queryの最適化
        if (context.IsPullQuery)
        {
            // Pull Queryには適切なインデックスヒント
            if (query.Contains("WHERE") && !query.Contains("LIMIT"))
            {
                Console.WriteLine("[KSQL-LINQ HINT] Consider adding LIMIT clause for Pull Query performance");
            }
        }

        // Push Queryの最適化
        if (!context.IsPullQuery)
        {
            // ストリーミングクエリのパフォーマンスヒント
            if (query.Contains("ORDER BY"))
            {
                Console.WriteLine("[KSQL-LINQ WARNING] ORDER BY in Push Queries may impact performance. Consider using windowing.");
            }

            if (!query.Contains("EMIT CHANGES"))
            {
                optimizedQuery = ApplyQueryPostProcessing(optimizedQuery, context);
            }
        }

        return optimizedQuery;
    }
}
