using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// JOINクエリ生成器（新規作成）
/// 設計理由：責務分離設計に準拠、2テーブルJOINとLEFT JOINに特化
/// </summary>
internal class JoinQueryGenerator : GeneratorBase
{
    /// <summary>
    /// コンストラクタ（Builder依存注入）
    /// </summary>
    public JoinQueryGenerator(IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> builders)
        : base(builders)
    {
    }

    /// <summary>
    /// 簡易コンストラクタ（標準Builder使用）
    /// </summary>
    public JoinQueryGenerator() : this(CreateStandardBuilders())
    {
    }

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return new[]
        {
            KsqlBuilderType.Join,
            KsqlBuilderType.Select,
            KsqlBuilderType.Where
        };
    }

    /// <summary>
    /// 2テーブルJOINクエリ生成
    /// </summary>
    public string GenerateTwoTableJoin(
        string outerTable,
        string innerTable,
        Expression outerKeySelector,
        Expression innerKeySelector,
        Expression? resultSelector = null,
        Expression? whereCondition = null,
        bool isPullQuery = true)
    {
        try
        {
            // JOIN制限チェック
            ValidateJoinConstraints(outerTable, innerTable);

            var context = new QueryAssemblyContext($"{outerTable}_JOIN_{innerTable}", isPullQuery);
            var structure = CreateJoinStructure(outerTable, innerTable);

            // JOIN条件構築
            var joinExpression = BuildJoinExpression(outerTable, innerTable, outerKeySelector, innerKeySelector, resultSelector);
            var joinContent = SafeCallBuilder(KsqlBuilderType.Join, joinExpression, "JOIN processing");

            // JOIN句は完全なクエリとして返される（JoinClauseBuilderが完全なSELECT文を生成）
            return ApplyQueryPostProcessing(joinContent, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("two-table JOIN generation", ex, $"Tables: {outerTable}, {innerTable}");
        }
    }

    /// <summary>
    /// LINQ JOIN式からクエリ生成
    /// </summary>
    public string GenerateFromLinqJoin(Expression joinExpression, bool isPullQuery = true)
    {
        try
        {
            // JOIN制限の事前チェック
            JoinLimitationEnforcer.ValidateJoinExpression(joinExpression);

            var context = new QueryAssemblyContext("LINQ_JOIN", isPullQuery);

            // JoinBuilderに委譲（完全なクエリを生成）
            var joinQuery = SafeCallBuilder(KsqlBuilderType.Join, joinExpression, "LINQ JOIN processing");

            return ApplyQueryPostProcessing(joinQuery, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("LINQ JOIN generation", ex, joinExpression.ToString());
        }
    }

    /// <summary>
    /// LEFT JOINクエリ生成（KSQL対応範囲内）
    /// </summary>
    public string GenerateLeftJoin(
        string outerTable,
        string innerTable,
        Expression outerKeySelector,
        Expression innerKeySelector,
        Expression? resultSelector = null,
        bool isPullQuery = true)
    {
        try
        {
            ValidateJoinConstraints(outerTable, innerTable);

            var context = new QueryAssemblyContext($"{outerTable}_LEFT_JOIN_{innerTable}", isPullQuery);

            // LEFT JOINの手動構築（KSQLの制限により）
            var query = BuildLeftJoinQuery(outerTable, innerTable, outerKeySelector, innerKeySelector, resultSelector);

            return ApplyQueryPostProcessing(query, context);
        }
        catch (System.Exception ex)
        {
            return HandleGenerationError("LEFT JOIN generation", ex, $"Tables: {outerTable}, {innerTable}");
        }
    }

    /// <summary>
    /// JOIN条件バリデーション
    /// </summary>
    private static void ValidateJoinConstraints(string outerTable, string innerTable)
    {
        if (string.IsNullOrWhiteSpace(outerTable))
            throw new ArgumentException("Outer table name cannot be null or empty");

        if (string.IsNullOrWhiteSpace(innerTable))
            throw new ArgumentException("Inner table name cannot be null or empty");

        if (outerTable.Equals(innerTable, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Cannot join table with itself in KSQL");
    }

    /// <summary>
    /// JOIN構造作成
    /// </summary>
    private static QueryStructure CreateJoinStructure(string outerTable, string innerTable)
    {
        var metadata = new QueryMetadata(DateTime.UtcNow, "DML", $"{outerTable}_JOIN_{innerTable}");
        return QueryStructure.CreateSelect($"{outerTable}_JOIN_{innerTable}").WithMetadata(metadata);
    }

    /// <summary>
    /// JOIN式構築
    /// </summary>
    private Expression BuildJoinExpression(
        string outerTable,
        string innerTable,
        Expression outerKeySelector,
        Expression innerKeySelector,
        Expression? resultSelector)
    {
        // キーセレクタのラムダを抽出し、型情報を取得
        var outerLambda = ExtractLambdaExpression(outerKeySelector)
            ?? throw new InvalidOperationException("Outer key selector must be a lambda expression");
        var innerLambda = ExtractLambdaExpression(innerKeySelector)
            ?? throw new InvalidOperationException("Inner key selector must be a lambda expression");

        var outerType = outerLambda.Parameters[0].Type;
        var innerType = innerLambda.Parameters[0].Type;
        var keyType = outerLambda.Body.Type;

        // IQueryable<> パラメータは型情報のみ利用
        var outerQueryable = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(outerType), "outer");
        var innerQueryable = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(innerType), "inner");

        var outerTypedLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(outerType, keyType),
            outerLambda.Body,
            outerLambda.Parameters);
        var innerTypedLambda = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(innerType, keyType),
            innerLambda.Body,
            innerLambda.Parameters);

        var joinMethod = typeof(Queryable).GetMethods()
            .First(m => m.Name == nameof(Queryable.Join) && m.GetParameters().Length == 5)
            .MakeGenericMethod(outerType, innerType, keyType, typeof(object));

        var defaultSelector = resultSelector ??
            CreateDefaultResultSelector(Expression.Parameter(outerType, outerLambda.Parameters[0].Name!),
                Expression.Parameter(innerType, innerLambda.Parameters[0].Name!));

        return Expression.Call(
            joinMethod,
            outerQueryable,
            innerQueryable,
            outerTypedLambda,
            innerTypedLambda,
            defaultSelector);
    }

    /// <summary>
    /// Lambda式抽出
    /// </summary>
    private static LambdaExpression? ExtractLambdaExpression(Expression expr)
    {
        return expr switch
        {
            UnaryExpression { Operand: LambdaExpression lambda } => lambda,
            LambdaExpression lambda => lambda,
            _ => null
        };
    }

    /// <summary>
    /// デフォルトResultSelector作成
    /// </summary>
    private static Expression CreateDefaultResultSelector(ParameterExpression outerParam, ParameterExpression innerParam)
    {
        // デフォルトは両テーブルの全カラムを返す
        var newExpression = Expression.New(typeof(object).GetConstructors()[0]);
        return Expression.Lambda(newExpression, outerParam, innerParam);
    }

    /// <summary>
    /// LEFT JOINクエリ構築
    /// </summary>
    private string BuildLeftJoinQuery(
        string outerTable,
        string innerTable,
        Expression outerKeySelector,
        Expression innerKeySelector,
        Expression? resultSelector)
    {
        var outerKeys = ExtractKeys(outerKeySelector);
        var innerKeys = ExtractKeys(innerKeySelector);

        var outerAlias = "o";
        var innerAlias = "i";

        var joinConditions = BuildJoinConditions(outerKeys, innerKeys, outerAlias, innerAlias);

        var projection = resultSelector != null ?
            ProcessResultSelector(resultSelector, outerAlias, innerAlias) :
            $"{outerAlias}.*, {innerAlias}.*";

        return $"SELECT {projection} " +
               $"FROM {outerTable} {outerAlias} " +
               $"LEFT JOIN {innerTable} {innerAlias} ON {joinConditions}";
    }

    /// <summary>
    /// キー抽出
    /// </summary>
    private List<string> ExtractKeys(Expression keySelector)
    {
        var keys = new List<string>();

        var lambdaBody = BuilderValidation.ExtractLambdaBody(keySelector);
        if (lambdaBody == null) return keys;

        switch (lambdaBody)
        {
            case NewExpression newExpr:
                foreach (var arg in newExpr.Arguments)
                {
                    if (arg is MemberExpression member)
                    {
                        keys.Add(member.Member.Name);
                    }
                }
                break;

            case MemberExpression member:
                keys.Add(member.Member.Name);
                break;
        }

        return keys;
    }

    /// <summary>
    /// JOIN条件構築
    /// </summary>
    private static string BuildJoinConditions(List<string> leftKeys, List<string> rightKeys, string leftAlias, string rightAlias)
    {
        if (leftKeys.Count != rightKeys.Count || leftKeys.Count == 0)
        {
            throw new InvalidOperationException("JOIN keys must match and cannot be empty");
        }

        var conditions = new List<string>();
        for (int i = 0; i < leftKeys.Count; i++)
        {
            conditions.Add($"{leftAlias}.{leftKeys[i]} = {rightAlias}.{rightKeys[i]}");
        }

        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// ResultSelector処理
    /// </summary>
    private string ProcessResultSelector(Expression resultSelector, params string[] tableAliases)
    {
        // 簡略化：実際の実装ではSelectClauseBuilderを使用
        var lambdaBody = BuilderValidation.ExtractLambdaBody(resultSelector);
        if (lambdaBody != null)
        {
            try
            {
                return SafeCallBuilder(KsqlBuilderType.Select, lambdaBody, "result selector processing");
            }
            catch
            {
                // フォールバック：全カラム返却
                return string.Join(", ", tableAliases.Select(alias => $"{alias}.*"));
            }
        }

        return string.Join(", ", tableAliases.Select(alias => $"{alias}.*"));
    }

    /// <summary>
    /// 標準Builder作成
    /// </summary>
    private static IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> CreateStandardBuilders()
    {
        return new Dictionary<KsqlBuilderType, IKsqlBuilder>
        {
            [KsqlBuilderType.Join] = new JoinClauseBuilder(),
            [KsqlBuilderType.Select] = new SelectClauseBuilder(),
            [KsqlBuilderType.Where] = new WhereClauseBuilder(),
            [KsqlBuilderType.GroupBy] = new GroupByClauseBuilder(),
            [KsqlBuilderType.Having] = new HavingClauseBuilder(),
        };
    }

    /// <summary>
    /// 最適化ヒント適用
    /// </summary>
    protected override string ApplyOptimizationHints(string query, QueryAssemblyContext context)
    {
        var optimizedQuery = query;

        // JOIN固有の最適化ヒント
        Console.WriteLine("[KSQL-LINQ HINT] Ensure joined tables are co-partitioned for optimal performance");

        if (query.Contains("LEFT JOIN"))
        {
            Console.WriteLine("[KSQL-LINQ HINT] LEFT JOIN may impact performance. Consider data denormalization if possible");
        }

        if (!context.IsPullQuery)
        {
            Console.WriteLine("[KSQL-LINQ HINT] Streaming JOINs require careful consideration of event-time vs processing-time semantics");
        }

        return optimizedQuery;
    }
}
