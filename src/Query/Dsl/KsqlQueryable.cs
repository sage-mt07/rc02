using System;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Dsl;

/// <summary>
/// Represents a queryable with a single source type.
/// Provides chaining methods for WHERE, SELECT, WINDOW and JOIN operations.
/// </summary>
public class KsqlQueryable<T1> : IKsqlQueryable, IScheduledScope<T1>
{
    private readonly KsqlQueryModel _model = new()
    {
        SourceTypes = new[] { typeof(T1) }
    };
    private QueryBuildStage _stage = QueryBuildStage.From;

    public KsqlQueryable<T1> Where(Expression<Func<T1, bool>> predicate)
    {
        if (_stage is QueryBuildStage.Select or QueryBuildStage.GroupBy or QueryBuildStage.Having)
            throw new InvalidOperationException("Where() must be called before GroupBy/Having/Select().");

        _model.WhereCondition = predicate;
        _stage = QueryBuildStage.Where;
        return this;
    }

    public KsqlQueryable<T1> Select<TResult>(Expression<Func<T1, TResult>> projection)
    {
        if (_stage == QueryBuildStage.Select)
            throw new InvalidOperationException("Select() has already been specified.");

        if (_stage == QueryBuildStage.Join || _stage == QueryBuildStage.From || _stage == QueryBuildStage.Where || _stage == QueryBuildStage.GroupBy || _stage == QueryBuildStage.Having)
        {
            _stage = QueryBuildStage.Select;
        }
        else
        {
            throw new InvalidOperationException("Select() cannot be called in the current state.");
        }

        _model.SelectProjection = projection;
        var visitor = new Kafka.Ksql.Linq.Query.Builders.AggregateDetectionVisitor();
        visitor.Visit(projection.Body);
        if (visitor.HasAggregates)
            _model.IsAggregateQuery = true;
        return this;
    }

    public KsqlGroupedQueryable<T1, TKey> GroupBy<TKey>(Expression<Func<T1, TKey>> keySelector)
    {
        if (_stage == QueryBuildStage.Select)
            throw new InvalidOperationException("GroupBy() must be called before Select().");

        _model.GroupByExpression = keySelector;
        _stage = QueryBuildStage.GroupBy;
        _model.IsAggregateQuery = true;
        return new KsqlGroupedQueryable<T1, TKey>(_model);
    }

    public KsqlQueryable<T1> Tumbling(
        Expression<Func<T1, DateTime>> time,
        int[]? minutes = null,
        int[]? hours = null,
        int[]? days = null,
        int[]? months = null,
        DayOfWeek? week = null,
        TimeSpan? grace = null)
    {
        _model.HasTumbling = true;
        if (minutes != null) foreach (var m in minutes) _model.Windows.Add($"{m}m");
        if (hours != null) foreach (var h in hours) _model.Windows.Add($"{h}h");
        if (days != null) foreach (var d in days) _model.Windows.Add($"{d}d");
        if (months != null) foreach (var mo in months) _model.Windows.Add($"{mo}mo");
        if (week.HasValue)
        {
            _model.WeekAnchor = week.Value;
            _model.Windows.Add("1wk");
        }
        static int ToMinutes(string tf)
        {
            if (tf.EndsWith("mo")) return int.Parse(tf[..^2]) * 43200;
            if (tf.EndsWith("wk")) return int.Parse(tf[..^2]) * 10080;
            var unit = tf[^1];
            var val = int.Parse(tf[..^1]);
            return unit switch
            {
                'm' => val,
                'h' => val * 60,
                'd' => val * 1440,
                _ => val
            };
        }
        var ordered = _model.Windows.Distinct().OrderBy(ToMinutes).ToList();
        _model.Windows.Clear();
        _model.Windows.AddRange(ordered);
        return this;
    }

    public KsqlQueryable<T1> Tumbling(Expression<Func<T1, object>> timeProperty, TimeSpan size, TimeSpan? grace = null)
    {
        throw new NotSupportedException("Legacy Tumbling overload is not supported in this phase.");
    }

    public IScheduledScope<T1> TimeFrame<TSchedule>(
        Expression<Func<T1, TSchedule, bool>> predicate,
        Expression<Func<TSchedule, object>>? dayKey = null)
    {
        _model.BasedOnType = typeof(TSchedule);
        _model.BasedOnDayKey = dayKey;
        return this;
    }

    public KsqlQueryable<T1> AsPush()
    {
        _model.ExecutionMode = QueryExecutionMode.PushQuery;
        return this;
    }

    public KsqlQueryable<T1> AsPull()
    {
        _model.ExecutionMode = QueryExecutionMode.PullQuery;
        return this;
    }

    public KsqlQueryable2<T1, T2> Join<T2>(Expression<Func<T1, T2, bool>> condition)
    {
        if (_stage != QueryBuildStage.From)
            throw new InvalidOperationException("Join() must be called immediately after From().");

        var newModel = new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(T1), typeof(T2) },
            JoinCondition = condition,
            WhereCondition = _model.WhereCondition,
            SelectProjection = _model.SelectProjection,
            IsAggregateQuery = _model.IsAggregateQuery
        };
        return new KsqlQueryable2<T1, T2>(newModel);
    }

    public KsqlQueryModel Build() => _model;

    private static string ExtractPropertyName(Expression expression)
    {
        return expression is LambdaExpression lambda && lambda.Body is MemberExpression member
            ? member.Member.Name
            : throw new ArgumentException("The timestamp property must be specified using a property access expression.");
    }
}
