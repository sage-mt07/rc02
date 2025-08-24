using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Common;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Ddl;
using Kafka.Ksql.Linq.Query.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using Kafka.Ksql.Linq.Configuration;
using System.Linq.Expressions;
using System.Reflection;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// DDLクエリ生成器（新Builder使用版）
/// 設計理由：責務分離設計に準拠、Builder統合型でCREATE STREAM/TABLE文生成
/// </summary>
internal class DDLQueryGenerator : GeneratorBase, IDDLQueryGenerator
{
    /// <summary>
    /// コンストラクタ（Builder依存注入）
    /// </summary>
    public DDLQueryGenerator(IReadOnlyDictionary<KsqlBuilderType, IKsqlBuilder> builders)
        : base(builders)
    {
    }

    /// <summary>
    /// 簡易コンストラクタ（標準Builder使用）
    /// </summary>
    public DDLQueryGenerator() : this(CreateStandardBuilders())
    {
    }

    protected override KsqlBuilderType[] GetRequiredBuilderTypes()
    {
        return new[]
        {
            KsqlBuilderType.Select,
            KsqlBuilderType.Where,
            KsqlBuilderType.GroupBy,
        };
    }

    private static string SanitizeName(string name) => name.Replace("-", "_");

    /// <summary>
    /// CREATE STREAM文生成
    /// </summary>
    public string GenerateCreateStream(IDdlSchemaProvider provider)
    {
        ModelCreatingScope.EnsureInScope();
        DdlSchemaDefinition? schema = null;
        try
        {
            schema = provider.GetSchema();
            var columns = GenerateColumnDefinitions(schema);
            var partitions = schema.Partitions;
            var replicas = schema.Replicas;
            var streamName = SanitizeName(schema.TopicName);
            var topicName = schema.TopicName;
            var hasKey = schema.Columns.Any(c => c.IsKey);

            var withParts = new List<string> { $"KAFKA_TOPIC='{topicName}'" };
            if (hasKey)
            {
                withParts.Add("KEY_FORMAT='AVRO'");
                if (schema.KeySchemaId.HasValue)
                    withParts.Add($"KEY_SCHEMA_ID={schema.KeySchemaId.Value}");
            }
            withParts.Add("VALUE_FORMAT='AVRO'");
            if (schema.ValueSchemaId.HasValue)
                withParts.Add($"VALUE_SCHEMA_ID={schema.ValueSchemaId.Value}");
            withParts.Add($"PARTITIONS={partitions}");
            withParts.Add($"REPLICAS={replicas}");
            var withClause = string.Join(", ", withParts);

            // ★ KSQLルール対応：カラム定義とSCHEMA_ID系は排他
            bool useSchemaId = schema.ValueSchemaId.HasValue || schema.KeySchemaId.HasValue;
            string query;
            if (useSchemaId)
            {
                // カラム定義部なし
                query = $"CREATE STREAM IF NOT EXISTS {streamName} WITH ({withClause})";
            }
            else
            {
                // カラム定義部あり
                query = $"CREATE STREAM IF NOT EXISTS {streamName} ({columns}) WITH ({withClause})";
            }

            if (!query.TrimEnd().EndsWith(";"))
            {
                query += ";";
            }

            return query;
        }
        catch (Exception ex)
        {
            return HandleGenerationError("CREATE STREAM generation", ex, $"Stream: {schema?.ObjectName}, Topic: {schema?.TopicName}");
        }
    }

    /// <summary>
    /// CREATE TABLE文生成
    /// </summary>
    public string GenerateCreateTable(IDdlSchemaProvider provider)
    {
        ModelCreatingScope.EnsureInScope();
        DdlSchemaDefinition? schema = null;
        try
        {
            schema = provider.GetSchema();
            var columns = GenerateColumnDefinitions(schema);
            var partitions = schema.Partitions;
            var replicas = schema.Replicas;
            var tableName = SanitizeName(schema.TopicName);
            var topicName = schema.TopicName;
            var hasKey = schema.Columns.Any(c => c.IsKey);

            var withParts = new List<string> { $"KAFKA_TOPIC='{topicName}'" };
            if (hasKey)
            {
                withParts.Add("KEY_FORMAT='AVRO'");
                if (schema.KeySchemaId.HasValue)
                    withParts.Add($"KEY_SCHEMA_ID={schema.KeySchemaId.Value}");
            }
            withParts.Add("VALUE_FORMAT='AVRO'");
            if (schema.ValueSchemaId.HasValue)
                withParts.Add($"VALUE_SCHEMA_ID={schema.ValueSchemaId.Value}");
            withParts.Add($"PARTITIONS={partitions}");
            withParts.Add($"REPLICAS={replicas}");
            var withClause = string.Join(", ", withParts);

            // --- KSQLルール対応（カラム定義 vs スキーマID系は排他） ---
            bool useSchemaId = schema.KeySchemaId.HasValue || schema.ValueSchemaId.HasValue;
            string query;
            if (useSchemaId)
            {
                // カラム定義部なし
                query = $"CREATE TABLE IF NOT EXISTS {tableName} WITH ({withClause})";
            }
            else
            {
                // カラム定義部あり
                query = $"CREATE TABLE IF NOT EXISTS {tableName} ({columns}) WITH ({withClause})";
            }

            if (!query.TrimEnd().EndsWith(";"))
            {
                query += ";";
            }

            return query;
        }
        catch (Exception ex)
        {
            return HandleGenerationError("CREATE TABLE generation", ex, $"Table: {schema?.ObjectName}, Topic: {schema?.TopicName}");
        }
    }
    /// <summary>
    /// CREATE STREAM AS文生成
    /// </summary>
    public string GenerateCreateStreamAs(string streamName, string baseObject, Expression linqExpression)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(baseObject, false); // Push Query
            var structure = CreateStreamAsStructure(streamName, baseObject);

            // LINQ式を解析してクエリ句を構築
            structure = ProcessLinqExpression(structure, linqExpression, context);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (Exception ex)
        {
            return HandleGenerationError("CREATE STREAM AS generation", ex, $"Stream: {streamName}, Base: {baseObject}");
        }
    }

    /// <summary>
    /// CREATE TABLE AS文生成
    /// </summary>
    public string GenerateCreateTableAs(string tableName, string baseObject, Expression linqExpression)
    {
        ModelCreatingScope.EnsureInScope();
        try
        {
            var context = new QueryAssemblyContext(baseObject, false); // Push Query
            var structure = CreateTableAsStructure(tableName, baseObject);

            // LINQ式を解析してクエリ句を構築
            structure = ProcessLinqExpression(structure, linqExpression, context);

            var query = AssembleStructuredQuery(structure);
            return ApplyQueryPostProcessing(query, context);
        }
        catch (Exception ex)
        {
            return HandleGenerationError("CREATE TABLE AS generation", ex, $"Table: {tableName}, Base: {baseObject}");
        }
    }

    /// <summary>
    /// カラム定義生成
    /// </summary>
    private string GenerateColumnDefinitions(DdlSchemaDefinition schema)
    {
        var keyColumns = schema.Columns.Where(c => c.IsKey).ToList();
        var nonKeyColumns = schema.Columns.Where(c => !c.IsKey).ToList();

        var columns = new List<string>();

        if (keyColumns.Count > 1)
        {
            var fields = keyColumns.Select(c => $"{c.Name} {c.Type}");
            var structDef = $"STRUCT<{string.Join(", ", fields)}>";
            var keyColumnName = $"{schema.ObjectName}_key";
            columns.Add($"{keyColumnName} {structDef} PRIMARY KEY");
            columns.AddRange(nonKeyColumns.Select(c => $"{c.Name} {c.Type}"));
        }
        else
        {
            foreach (var column in schema.Columns)
            {
                var definition = $"{column.Name} {column.Type}";
                if (column.IsKey)
                {
                    definition += " PRIMARY KEY";
                }
                columns.Add(definition);
            }
        }

        return string.Join(", ", columns);
    }

    /// <summary>
    /// CREATE STREAM AS構造作成
    /// </summary>
    private static QueryStructure CreateStreamAsStructure(string streamName, string baseObject)
    {
        var metadata = new QueryMetadata(DateTime.UtcNow, "DDL", baseObject);
        var structure = QueryStructure.CreateStreamAs(streamName, baseObject).WithMetadata(metadata);
        var fromClause = QueryClause.Required(QueryClauseType.From, $"FROM {baseObject}");
        return structure.AddClause(fromClause);
    }

    /// <summary>
    /// CREATE TABLE AS構造作成
    /// </summary>
    private static QueryStructure CreateTableAsStructure(string tableName, string baseObject)
    {
        var metadata = new QueryMetadata(DateTime.UtcNow, "DDL", baseObject);
        var structure = QueryStructure.CreateTableAs(tableName, baseObject).WithMetadata(metadata);
        var fromClause = QueryClause.Required(QueryClauseType.From, $"FROM {baseObject}");
        return structure.AddClause(fromClause);
    }

    /// <summary>
    /// LINQ式処理
    /// </summary>
    private QueryStructure ProcessLinqExpression(QueryStructure structure, Expression linqExpression, QueryAssemblyContext context)
    {
        var analysis = AnalyzeLinqExpression(linqExpression);
        var metadata = structure.Metadata;
        var analysisMd = analysis.ToMetadata();
        if (analysisMd.Properties is { } props)
        {
            foreach (var kv in props)
                metadata = metadata.WithProperty(kv.Key, kv.Value);
        }
        structure = structure.WithMetadata(metadata);

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
            "Join" => ProcessJoinMethod(structure, methodCall),
            "Having" => ProcessHavingMethod(structure, methodCall),
            "OrderBy" or "OrderByDescending" or "ThenBy" or "ThenByDescending" => ProcessOrderByMethod(structure, methodCall),
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
                var whereContent = SafeCallBuilder(KsqlBuilderType.Where, lambdaBody, "WHERE processing");
                var clause = QueryClause.Required(QueryClauseType.Where, $"WHERE {whereContent}", lambdaBody);
                structure = structure.AddClause(clause);
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
            structure = structure.AddClause(clause);
        }

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
        };
    }
}
