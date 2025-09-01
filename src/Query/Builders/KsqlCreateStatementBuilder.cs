using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kafka.Ksql.Linq.Query.Builders;

public static class KsqlCreateStatementBuilder
{
    public static string Build(string streamName, KsqlQueryModel model, string? keySchemaFullName = null, string? valueSchemaFullName = null, bool includeKey = false, string? partitionBy = null)
    {
        return Build(streamName, model, keySchemaFullName, valueSchemaFullName, ResolveSourceName, includeKey, partitionBy);
    }

    /// <summary>
    /// Build a CREATE statement with an optional source name resolver for FROM/JOIN tables.
    /// </summary>
    public static string Build(string streamName, KsqlQueryModel model, string? keySchemaFullName, string? valueSchemaFullName, Func<Type, string> sourceNameResolver, bool includeKey = false, string? partitionBy = null)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            throw new ArgumentException("Stream name is required", nameof(streamName));
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        string selectClause;
        if (model.SelectProjection == null)
        {
            selectClause = "*";
        }
        else
        {
            // Map projection parameter names to resolved source names for qualification
            var map = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
            var parameters = model.SelectProjection.Parameters;
            for (int i = 0; i < parameters.Count && i < (model.SourceTypes?.Length ?? 0); i++)
            {
                var pname = parameters[i].Name ?? string.Empty;
                // Use the same aliases as FROM/JOIN (o/i) for qualification
                var alias = i == 0 ? "o" : "i";
                map[pname] = alias;
            }
            var builder = new SelectClauseBuilder(map);
            selectClause = builder.Build(model.SelectProjection.Body);
        }
        var fromClause = BuildFromClauseCore(model, sourceNameResolver);
        var whereClause = BuildWhereClause(model.WhereCondition);
        var groupByClause = BuildGroupByClause(model.GroupByExpression);
        var havingClause = BuildHavingClause(model.HavingCondition);

        var createType = model.IsAggregateQuery ? "CREATE TABLE" : "CREATE STREAM";

        var sb = new StringBuilder();
        sb.Append($"{createType} {streamName}");
        if (includeKey || !string.IsNullOrWhiteSpace(keySchemaFullName) || !string.IsNullOrWhiteSpace(valueSchemaFullName))
        {
            var withParts = new List<string> { $"KAFKA_TOPIC='{streamName}'" };
            if (includeKey && !string.IsNullOrWhiteSpace(keySchemaFullName))
            {
                withParts.Add("KEY_FORMAT='AVRO'");
                withParts.Add($"KEY_AVRO_SCHEMA_FULL_NAME='{keySchemaFullName}'");
            }
            withParts.Add("VALUE_FORMAT='AVRO'");
            if (!string.IsNullOrWhiteSpace(valueSchemaFullName))
                withParts.Add($"VALUE_AVRO_SCHEMA_FULL_NAME='{valueSchemaFullName}'");
            sb.Append(" WITH (" + string.Join(", ", withParts) + ")");
        }
        sb.AppendLine(" AS");
        sb.AppendLine($"SELECT {selectClause}");
        sb.Append(fromClause);
        if (!string.IsNullOrEmpty(whereClause))
        {
            sb.AppendLine();
            sb.Append(whereClause);
        }
        if (!string.IsNullOrEmpty(groupByClause))
        {
            sb.AppendLine();
            sb.Append(groupByClause);
        }
        if (!string.IsNullOrEmpty(havingClause))
        {
            sb.AppendLine();
            sb.Append(havingClause);
        }
        if (!string.IsNullOrEmpty(partitionBy))
        {
            sb.AppendLine();
            sb.Append($"PARTITION BY {partitionBy}");
        }
        var mode = model.ExecutionMode == Query.Pipeline.QueryExecutionMode.Unspecified
            ? Query.Pipeline.QueryExecutionMode.PushQuery
            : model.ExecutionMode;
        if (mode == Query.Pipeline.QueryExecutionMode.PushQuery)
        {
            sb.AppendLine();
            sb.Append("EMIT CHANGES;");
        }
        else
        {
            sb.Append(';');
        }
        return sb.ToString();
    }

    private static string BuildFromClauseCore(KsqlQueryModel model, Func<Type, string>? sourceNameResolver)
    {
        var types = model.SourceTypes;
        if (types == null || types.Length == 0)
            throw new InvalidOperationException("Source types are required");

        if (types.Length > 2)
            throw new NotSupportedException("Only up to 2 tables are supported in JOIN");

        var result = new StringBuilder();
        var left = sourceNameResolver?.Invoke(types[0]) ?? ResolveSourceName(types[0]);
        var lAlias = "o"; // explicit alias for left source
        result.Append($"FROM {left} {lAlias}");

        if (types.Length > 1)
        {
            var right = sourceNameResolver?.Invoke(types[1]) ?? ResolveSourceName(types[1]);
            var rAlias = "i"; // explicit alias for right source
            result.Append($" JOIN {right} {rAlias}");
            if (model.JoinCondition == null)
                throw new InvalidOperationException("Join condition required for two table join");

            // Enforce WITHIN for stream-stream joins: require WithinSeconds
            if (!model.WithinSeconds.HasValue || model.WithinSeconds.Value <= 0)
                throw new InvalidOperationException("Stream-Stream JOIN requires .Within(seconds) (e.g. Within(60)).");
            result.Append($" WITHIN {model.WithinSeconds.Value} SECONDS");

            // Build a qualified join condition using aliases to avoid ambiguity
            var condition = BuildQualifiedJoinCondition(model.JoinCondition, lAlias, rAlias);
            result.Append($" ON {condition}");
        }

        return result.ToString();
    }

    private static string BuildQualifiedJoinCondition(LambdaExpression joinExpr, string leftAlias, string rightAlias)
    {
        string Build(Expression expr)
        {
            switch (expr)
            {
                case BinaryExpression be when be.NodeType == ExpressionType.Equal:
                    return $"({Build(be.Left)} = {Build(be.Right)})";
                case MemberExpression me:
                {
                    var param = GetRootParameter(me);
                    if (param != null)
                    {
                        if (joinExpr.Parameters.Count > 0 && param == joinExpr.Parameters[0])
                        {
                            var col = me.Member.Name;
                            if (!col.StartsWith("`")) col = $"`{col}`";
                            return $"{leftAlias}.{col}";
                        }
                        if (joinExpr.Parameters.Count > 1 && param == joinExpr.Parameters[1])
                        {
                            var col = me.Member.Name;
                            if (!col.StartsWith("`")) col = $"`{col}`";
                            return $"{rightAlias}.{col}";
                        }
                    }
                    throw new InvalidOperationException("Unqualified column access in JOIN condition is not allowed.");
                }
                case UnaryExpression ue:
                    return Build(ue.Operand);
                case ConstantExpression ce:
                    return Builders.Common.BuilderValidation.SafeToString(ce.Value);
                default:
                    return expr.ToString();
            }
        }

        static ParameterExpression? GetRootParameter(MemberExpression me)
        {
            Expression? e = me.Expression;
            while (e is MemberExpression m)
                e = m.Expression;
            return e as ParameterExpression;
        }

        return Build(joinExpr.Body);
    }

    private static string ResolveSourceName(Type type)
    {
        // If the entity type has [KsqlTopic("name")], use that (uppercased for KSQL identifiers)
        var attr = type.GetCustomAttributes(true).OfType<Kafka.Ksql.Linq.Core.Attributes.KsqlTopicAttribute>().FirstOrDefault();
        if (attr != null && !string.IsNullOrWhiteSpace(attr.Name))
            return attr.Name.ToUpperInvariant();
        return type.Name;
    }

    private static string BuildWhereClause(LambdaExpression? where)
    {
        if (where == null) return string.Empty;
        var builder = new WhereClauseBuilder();
        var condition = builder.Build(where.Body);
        return $"WHERE {condition}";
    }

    private static string BuildGroupByClause(LambdaExpression? groupBy)
    {
        if (groupBy == null) return string.Empty;
        var builder = new GroupByClauseBuilder();
        var keys = builder.Build(groupBy.Body);
        return $"GROUP BY {keys}";
    }

    private static string BuildHavingClause(LambdaExpression? having)
    {
        if (having == null) return string.Empty;
        var builder = new HavingClauseBuilder();
        var condition = builder.Build(having.Body);
        return $"HAVING {condition}";
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalDays >= 1 && timeSpan.TotalDays == Math.Floor(timeSpan.TotalDays))
            return $"{(int)timeSpan.TotalDays} DAYS";
        if (timeSpan.TotalHours >= 1 && timeSpan.TotalHours == Math.Floor(timeSpan.TotalHours))
            return $"{(int)timeSpan.TotalHours} HOURS";
        if (timeSpan.TotalMinutes >= 1 && timeSpan.TotalMinutes == Math.Floor(timeSpan.TotalMinutes))
            return $"{(int)timeSpan.TotalMinutes} MINUTES";
        if (timeSpan.TotalSeconds >= 1 && timeSpan.TotalSeconds == Math.Floor(timeSpan.TotalSeconds))
            return $"{(int)timeSpan.TotalSeconds} SECONDS";
        if (timeSpan.TotalMilliseconds >= 1)
            return $"{(int)timeSpan.TotalMilliseconds} MILLISECONDS";
        return "0 SECONDS";
    }
}
