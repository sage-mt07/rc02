using System;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Tests;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders;

public class GroupByClauseBuilderTests
{
    [Fact]
    public void Build_SingleKey_ReturnsKeyName()
    {
        Expression<Func<TestEntity, object>> expr = e => e.Type;
        var builder = new GroupByClauseBuilder();
        var sql = builder.Build(expr.Body);
        Assert.Equal("Type", sql);
    }

    [Fact]
    public void Build_CompositeKey_ReturnsCommaSeparatedKeys()
    {
        Expression<Func<TestEntity, object>> expr = e => new { e.Id, e.Type };
        var builder = new GroupByClauseBuilder();
        var sql = builder.Build(expr.Body);
        Assert.Equal("Id, Type", sql);
    }
}
