using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Builders;
using System;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class KsqlCreateStatementBuilderDslTests
{
    private class Order { public int Id { get; set; } public int CustomerId { get; set; } }
    private class Customer { public int Id { get; set; } public bool IsActive { get; set; } public string Name { get; set; } = string.Empty; }

    [Fact]
    public void Build_WithJoinWhereSelect_GeneratesKsql()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where((o, c) => c.IsActive)
            .Select((o, c) => new { o.Id, c.Name })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("JoinView", model, 1, 2);
        Assert.Contains("JOIN Customer", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("SELECT", sql);
        Assert.Contains("KEY_SCHEMA_ID=1", sql);
        Assert.Contains("VALUE_SCHEMA_ID=2", sql);
    }

    [Fact]
    public void Build_JoinWithoutWhere_GeneratesSql()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Select((o, c) => new { o.Id, c.Name })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("JoinView", model);
        Assert.Contains("JOIN Customer", sql);
        Assert.DoesNotContain("WHERE", sql);
    }
}
