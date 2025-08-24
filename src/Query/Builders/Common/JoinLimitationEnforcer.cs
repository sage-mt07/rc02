using Kafka.Ksql.Linq.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders.Common;

/// <summary>
/// JOIN制限強制クラス
/// 設計理由：ストリーム処理における2テーブル制限の厳格実装
/// </summary>
internal static class JoinLimitationEnforcer
{
    public const int MaxJoinTables = 2;

    /// <summary>
    /// JOIN式の検証
    /// </summary>
    public static void ValidateJoinExpression(Expression expression)
    {
        var joinCount = CountJoins(expression);
        var tableCount = joinCount + 1;
        if (tableCount > MaxJoinTables)
        {
            throw new StreamProcessingException(
                $"Stream processing supports maximum {MaxJoinTables} table joins. " +
                $"Found {tableCount} tables. " +
                $"Consider data denormalization or use batch processing for complex relationships. " +
                $"Alternative: Create materialized views or use event sourcing patterns.");
        }

        ValidateJoinTypes(expression);
    }

    /// <summary>
    /// JOIN型パターンの検証
    /// </summary>
    public static void ValidateJoinTypes(Expression expression)
    {
        var violations = DetectUnsupportedJoinPatterns(expression);
        if (violations.Any())
        {
            throw new StreamProcessingException(
                $"Unsupported join patterns detected: {string.Join(", ", violations)}. " +
                $"Supported: INNER, LEFT OUTER joins with co-partitioned data.");
        }
    }

    /// <summary>
    /// JOIN数のカウント
    /// </summary>
    private static int CountJoins(Expression expression)
    {
        var visitor = new JoinCountVisitor();
        visitor.Visit(expression);
        return visitor.JoinCount;
    }

    /// <summary>
    /// サポートされていないJOINパターンの検出
    /// </summary>
    private static List<string> DetectUnsupportedJoinPatterns(Expression expression)
    {
        var visitor = new UnsupportedJoinPatternVisitor();
        visitor.Visit(expression);
        return visitor.Violations;
    }

    /// <summary>
    /// JOIN実行時制約の検証
    /// </summary>
    public static void ValidateJoinConstraints(
        Type outerType,
        Type innerType,
        Expression outerKeySelector,
        Expression innerKeySelector)
    {
        // キー型一致性チェック
        var outerKeyType = ExtractKeyType(outerKeySelector);
        var innerKeyType = ExtractKeyType(innerKeySelector);

        if (outerKeyType != null && innerKeyType != null && outerKeyType != innerKeyType)
        {
            throw new StreamProcessingException(
                $"JOIN key types must match. Outer key: {outerKeyType.Name}, Inner key: {innerKeyType.Name}. " +
                $"Ensure both tables are partitioned by the same key type for optimal performance.");
        }

        // パーティション推奨警告
        ValidatePartitioningRecommendations(outerType, innerType);
    }

    /// <summary>
    /// パーティショニング推奨事項の検証
    /// </summary>
    private static void ValidatePartitioningRecommendations(Type outerType, Type innerType)
    {
        // 実装簡略化版（実際の環境では詳細なパーティション情報が必要）
        var outerTopicName = GetTopicName(outerType);
        var innerTopicName = GetTopicName(innerType);

        if (outerTopicName != null && innerTopicName != null)
        {
            // パーティション数やキー分散の警告
            // 本来はメタデータストアから情報取得
            ConsoleWarningIfNeeded(outerTopicName, innerTopicName);
        }
    }

    /// <summary>
    /// キー型抽出
    /// </summary>
    private static Type? ExtractKeyType(Expression keySelector)
    {
        if (keySelector is LambdaExpression lambda)
        {
            return lambda.ReturnType;
        }

        return keySelector.Type;
    }

    /// <summary>
    /// トピック名取得
    /// </summary>
    private static string? GetTopicName(Type entityType)
    {
        return entityType.Name;
    }

    /// <summary>
    /// パフォーマンス警告
    /// </summary>
    private static void ConsoleWarningIfNeeded(string outerTopic, string innerTopic)
    {
        // 開発環境での警告出力（本番では適切なロギングフレームワーク使用）
        Console.WriteLine($"[KSQL-LINQ WARNING] JOIN performance optimization: " +
            $"Ensure topics '{outerTopic}' and '{innerTopic}' have same partition count and key distribution.");
    }

    /// <summary>
    /// JOINカウンター用Visitor
    /// </summary>
    private class JoinCountVisitor : ExpressionVisitor
    {
        public int JoinCount { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == "Join")
            {
                JoinCount++;

                // ネストしたJOINもカウント
                if (node.Object != null)
                    Visit(node.Object);

                foreach (var arg in node.Arguments)
                    Visit(arg);

                return node;
            }

            return base.VisitMethodCall(node);
        }
    }

    /// <summary>
    /// サポートされていないJOINパターン検出用Visitor
    /// </summary>
    private class UnsupportedJoinPatternVisitor : ExpressionVisitor
    {
        public List<string> Violations { get; } = new();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodName = node.Method.Name;

            // LINQ Join以外のJOIN系メソッド検出
            switch (methodName)
            {
                case "GroupJoin":
                    Violations.Add("GROUP JOIN (use regular JOIN with GROUP BY instead)");
                    break;

                case "FullOuterJoin":
                    Violations.Add("FULL OUTER JOIN (not supported in KSQL)");
                    break;

                case "RightJoin":
                    Violations.Add("RIGHT JOIN (use LEFT JOIN with swapped operands)");
                    break;

                case "CrossJoin":
                    Violations.Add("CROSS JOIN (performance risk in streaming)");
                    break;
            }

            return base.VisitMethodCall(node);
        }
    }
}
