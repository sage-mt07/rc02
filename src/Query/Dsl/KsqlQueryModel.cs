using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Dsl;

public class KsqlQueryModel
{
    public Type[] SourceTypes { get; init; } = Array.Empty<Type>();
    public LambdaExpression? JoinCondition { get; set; }
    public LambdaExpression? WhereCondition { get; set; }
    public LambdaExpression? SelectProjection { get; set; }
    public LambdaExpression? GroupByExpression { get; set; }
    public LambdaExpression? HavingCondition { get; set; }
    public bool IsAggregateQuery { get; set; }
    public QueryExecutionMode ExecutionMode { get; set; } = QueryExecutionMode.Unspecified;
    public bool HasTumbling { get; set; }
    public Type? BasedOnType { get; set; }
    public LambdaExpression? BasedOnDayKey { get; set; }
    public List<string> Windows { get; } = new();
    public DayOfWeek WeekAnchor { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Returns a simple string representation useful for debugging.
    /// </summary>
    public string Dump()
    {
        var sources = string.Join(",", SourceTypes.Select(t => t.Name));
        return $"Sources:[{sources}] Join:{JoinCondition} Where:{WhereCondition} Select:{SelectProjection} Aggregate:{IsAggregateQuery} Mode:{ExecutionMode}";
    }
}
