using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Tests;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Builders.Visitors;

public class SelectExpressionVisitorKeyDuplicateTests
{
    [Fact]
    public void VisitNew_DuplicateGroupKey_IgnoresDuplicate()
    {
        Expression<Func<TestEntity, object>> groupExpr = e => e.Id;
        var groupBuilder = new GroupByClauseBuilder();
        groupBuilder.Build(groupExpr.Body);

        Expression<Func<IGrouping<int, TestEntity>, object>> select = g => new { g.Key, Dup = g.Key };
        var visitor = new SelectExpressionVisitor();
        visitor.Visit(select.Body);
        var result = visitor.GetResult();
        Assert.Equal("Id", result);
    }
}
