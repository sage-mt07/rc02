using Kafka.Ksql.Linq.Query.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

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
    public int? WithinSeconds { get; set; }
    public bool ForbidDefaultWithin { get; set; }
    public bool IsFinal { get; set; }
    public int? GraceSeconds { get; set; }
    public System.Collections.Generic.Dictionary<string, object?> Extras { get; } = new();

    public KsqlQueryModel Clone()
    {
        var clone = new KsqlQueryModel
        {
            SourceTypes = (Type[])SourceTypes.Clone(),
            JoinCondition = JoinCondition,
            WhereCondition = WhereCondition,
            SelectProjection = SelectProjection,
            GroupByExpression = GroupByExpression,
            HavingCondition = HavingCondition,
            IsAggregateQuery = IsAggregateQuery,
            ExecutionMode = ExecutionMode,
            HasTumbling = HasTumbling,
            BasedOnType = BasedOnType,
            BasedOnDayKey = BasedOnDayKey,
            WeekAnchor = WeekAnchor,
            WithinSeconds = WithinSeconds,
            ForbidDefaultWithin = ForbidDefaultWithin,
            IsFinal = IsFinal,
            GraceSeconds = GraceSeconds
        };
        clone.Windows.AddRange(Windows);
        foreach (var kv in Extras)
            clone.Extras[kv.Key] = kv.Value;
        return clone;
    }

    /// <summary>
    /// Returns a simple string representation useful for debugging.
    /// </summary>
    public string Dump()
    {
        var sources = string.Join(",", SourceTypes.Select(t => t.Name));
        return $"Sources:[{sources}] Join:{JoinCondition} Where:{WhereCondition} Select:{SelectProjection} Aggregate:{IsAggregateQuery} Mode:{ExecutionMode}";
    }
}
