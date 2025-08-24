using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Kafka.Ksql.Linq.Query.Builders;

public static class KsqlCreateStatementBuilder
{
    public static string Build(string streamName, KsqlQueryModel model, int? keySchemaId = null, int? valueSchemaId = null)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            throw new ArgumentException("Stream name is required", nameof(streamName));
        if (model == null)
            throw new ArgumentNullException(nameof(model));


        var selectClause = BuildSelectClause(model.SelectProjection);
        var fromClause = BuildFromClause(model);
        var whereClause = BuildWhereClause(model.WhereCondition);
        var groupByClause = BuildGroupByClause(model.GroupByExpression);
        var havingClause = BuildHavingClause(model.HavingCondition);

        var createType = model.IsAggregateQuery ? "CREATE TABLE" : "CREATE STREAM";

        var sb = new StringBuilder();
        sb.Append($"{createType} {streamName}");
        if (keySchemaId.HasValue || valueSchemaId.HasValue)
        {
            var withParts = new List<string> { $"KAFKA_TOPIC='{streamName}'" };
            if (keySchemaId.HasValue)
            {
                withParts.Add("KEY_FORMAT='AVRO'");
                withParts.Add($"KEY_SCHEMA_ID={keySchemaId.Value}");
            }
            withParts.Add("VALUE_FORMAT='AVRO'");
            if (valueSchemaId.HasValue)
                withParts.Add($"VALUE_SCHEMA_ID={valueSchemaId.Value}");
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

    private static string BuildSelectClause(LambdaExpression? projection)
    {
        if (projection == null)
            return "*";

        var builder = new SelectClauseBuilder();
        return builder.Build(projection.Body);
    }

    private static string BuildFromClause(KsqlQueryModel model)
    {
        var types = model.SourceTypes;
        if (types == null || types.Length == 0)
            throw new InvalidOperationException("Source types are required");

        if (types.Length > 2)
            throw new NotSupportedException("Only up to 2 tables are supported in JOIN");

        var result = new StringBuilder();
        result.Append($"FROM {types[0].Name}");

        if (types.Length > 1)
        {
            result.Append($" JOIN {types[1].Name}");
            if (model.JoinCondition == null)
                throw new InvalidOperationException("Join condition required for two table join");

            var whereBuilder = new WhereClauseBuilder();
            var condition = whereBuilder.Build(model.JoinCondition.Body);
            result.Append($" ON {condition}");
        }

        return result.ToString();
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
