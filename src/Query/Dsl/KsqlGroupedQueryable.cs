using System;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Dsl;

/// <summary>
/// Represents a grouped queryable after GroupBy().
/// </summary>
public class KsqlGroupedQueryable<T, TKey> : IKsqlQueryable
{
    private readonly KsqlQueryModel _model;
    private QueryBuildStage _stage = QueryBuildStage.GroupBy;

    internal KsqlGroupedQueryable(KsqlQueryModel model)
    {
        _model = model;
    }

    public KsqlGroupedQueryable<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
    {
        if (_stage != QueryBuildStage.GroupBy)
            throw new InvalidOperationException("Having() must be called immediately after GroupBy().");

        _model.HavingCondition = predicate;
        _stage = QueryBuildStage.Having;
        return this;
    }

    public KsqlGroupedQueryable<T, TKey> Select<TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> projection)
    {
        if (_stage is not (QueryBuildStage.GroupBy or QueryBuildStage.Having))
            throw new InvalidOperationException("Select() must be called after GroupBy() and optional Having().");

        _model.SelectProjection = projection;
        _stage = QueryBuildStage.Select;
        var visitor = new Kafka.Ksql.Linq.Query.Builders.AggregateDetectionVisitor();
        visitor.Visit(projection.Body);
        if (visitor.HasAggregates)
            _model.IsAggregateQuery = true;
        return this;
    }

    public KsqlGroupedQueryable<T, TKey> AsPush()
    {
        _model.ExecutionMode = QueryExecutionMode.PushQuery;
        return this;
    }

    public KsqlGroupedQueryable<T, TKey> AsPull()
    {
        _model.ExecutionMode = QueryExecutionMode.PullQuery;
        return this;
    }

    public KsqlQueryModel Build() => _model;
}
