using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class KsqlInsertStatementBuilderTests
{
    private class Order { public int Id { get; set; } public int Amount { get; set; } }

    [Fact]
    public void Build_InsertSelect_GeneratesKsql()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id, o.Amount })
            .Build();

        var sql = KsqlInsertStatementBuilder.Build("orders", model);
        Assert.Contains("INSERT INTO orders", sql);
        Assert.Contains("SELECT", sql);
        Assert.Contains("EMIT CHANGES;", sql);
    }

    [Fact]
    public void Build_AlwaysAppendsEmitChanges()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id })
            .Build();
        model.ExecutionMode = QueryExecutionMode.PullQuery;

        var sql = KsqlInsertStatementBuilder.Build("orders", model);

        Assert.Contains("EMIT CHANGES;", sql);
    }
}
