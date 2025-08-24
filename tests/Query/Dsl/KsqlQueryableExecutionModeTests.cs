using System;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class KsqlQueryableExecutionModeTests
{
    private class Item { public int Id { get; set; } }
    private class Other { public int Id { get; set; } }

    [Fact]
    public void FromSelectAsPush_GeneratesEmitChanges()
    {
        var model = new KsqlQueryRoot()
            .From<Item>()
            .Select(i => new { i.Id })
            .AsPush()
            .Build();

        Assert.Equal(QueryExecutionMode.PushQuery, model.ExecutionMode);
        var sql = KsqlCreateStatementBuilder.Build("Test", model);
        Assert.Contains("EMIT CHANGES", sql);
    }

    [Fact]
    public void FromJoinSelectAsPull_OmitsEmitChanges()
    {
        var model = new KsqlQueryRoot()
            .From<Item>()
            .Join<Other>((i, o) => i.Id == o.Id)
            .Select((i, o) => new { i.Id })
            .AsPull()
            .Build();

        Assert.Equal(QueryExecutionMode.PullQuery, model.ExecutionMode);
        var sql = KsqlCreateStatementBuilder.Build("Test", model);
        Assert.DoesNotContain("EMIT CHANGES", sql);
    }

    [Fact]
    public void JoinAfterSelect_Throws()
    {
        var query = new KsqlQueryRoot()
            .From<Item>()
            .Select(i => new { i.Id });

        Assert.Throws<InvalidOperationException>(() =>
            query.Join<Other>((i, o) => i.Id == o.Id));
    }
}
