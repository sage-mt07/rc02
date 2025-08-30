using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Core.Modeling;
using System;
using System.Linq;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class ToQueryDslTests
{
    private class Order
    {
        [KsqlKey]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public double Amount { get; set; }
    }

    private class Customer
    {
        [KsqlKey]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private class OrderView
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class KeylessView
    {
        public string Name { get; set; } = string.Empty;
    }

    private class FakeQueryable : IKsqlQueryable
    {
        public KsqlQueryModel Build() => new KsqlQueryModel
        {
            SourceTypes = new[] { typeof(Order), typeof(Customer), typeof(OrderView) }
        };
    }

    [Fact]
    public void FromOnly_GeneratesSelectAll()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("FROM Order", sql);
        Assert.Contains("SELECT *", sql);
    }

    [Fact]
    public void FromSelect_GeneratesColumnList()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("SELECT Id", sql);
    }

    [Fact]
    public void JoinSelect_GeneratesJoinClause()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where((o, c) => c.IsActive)
            .Select((o, c) => new { o.Id, c.Name })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("view", model);
        Assert.Contains("JOIN Customer", sql);
        Assert.Contains("ON (CustomerId = Id)", sql);
    }

    [Fact]
    public void JoinWhereSelect_GeneratesWhere()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where((o, c) => c.IsActive)
            .Select((o, c) => new { o.Id, c.Name })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("view", model);
        Assert.Contains("WHERE", sql);
        Assert.Contains("IsActive", sql);
    }

    [Fact]
    public void AsPush_AddsEmitChanges()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id })
            .AsPush()
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("EMIT CHANGES", sql);
    }

    [Fact]
    public void AsPull_OmitsEmitChanges()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id })
            .AsPull()
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.DoesNotContain("EMIT CHANGES", sql);
    }

    [Fact]
    public void KeylessEntity_AllowsNoKeys()
    {
        var builder = new ModelBuilder();
        builder.Entity<Order>();
        var entityBuilder = builder.Entity<KeylessView>();

        entityBuilder.ToQuery(q => q.From<Order>()
            .Select(o => new KeylessView { Name = "x" }));

        var model = builder.GetEntityModel<KeylessView>()!;
        Assert.NotNull(model.QueryModel);
    }

    [Fact]
    public void KeyMismatch_Throws()
    {
        var builder = new ModelBuilder();
        builder.Entity<Order>();
        var entityBuilder = builder.Entity<OrderView>();

        Assert.Throws<InvalidOperationException>(() =>
            entityBuilder.ToQuery(q => q.From<Order>()
                .Select(o => new { o.CustomerId }))); 
    }

    [Fact]
    public void SelectOrder_AffectsSqlColumnOrder()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { Name = o.CustomerId.ToString(), Id = o.Id })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        var selectLine = sql.Split('\n')[1];
        Assert.Contains("Name", selectLine);
        var nameIndex = selectLine.IndexOf("Name");
        var idIndex = selectLine.IndexOf("Id", nameIndex + 1);
        Assert.True(nameIndex < idIndex);
    }

    [Fact]
    public void ThreeTableJoin_Throws()
    {
        var builder = new ModelBuilder();
        Assert.Throws<NotSupportedException>(() =>
            builder.Entity<OrderView>().ToQuery(_ => new FakeQueryable()));
    }

    [Fact]
    public void WhereAfterSelect_Throws()
    {
        var query = new KsqlQueryRoot()
            .From<Order>()
            .Select(o => new { o.Id });

        Assert.Throws<InvalidOperationException>(() =>
            query.Where(o => o.Id > 0));
    }

    [Fact]
    public void Tumbling_NotSupported()
    {
        var query = new KsqlQueryRoot()
            .From<Order>();

        Assert.Throws<NotSupportedException>(() =>
            query.Tumbling(o => o.Id, TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void GroupBySelect_GeneratesGroupByClause()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("GROUP BY CustomerId", sql);
        Assert.Contains("COUNT(", sql);
    }

    [Fact]
    public void GroupByHaving_GeneratesHavingClause()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .GroupBy(o => o.CustomerId)
            .Having(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("HAVING", sql);
        Assert.Contains("COUNT(*) > 1", sql);
    }

    [Fact]
    public void GroupBySelectWithCase_GeneratesCaseExpression()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Status = g.Sum(x => x.Amount) > 100 ? "VIP" : "Regular" })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        Assert.Contains("CASE WHEN", sql);
        Assert.Contains("SUM(", sql);
    }

    [Fact]
    public void HavingAfterSelect_Throws()
    {
        var query = new KsqlQueryRoot()
            .From<Order>()
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key });

        Assert.Throws<InvalidOperationException>(() =>
            query.Having(g => g.Count() > 1));
    }

    [Fact]
    public void SqlClauseOrder_IsCorrect()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Where(o => o.Amount > 0)
            .GroupBy(o => o.CustomerId)
            .Having(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .Build();

        var sql = KsqlCreateStatementBuilder.Build("orders", model);
        var fromIdx = sql.IndexOf("FROM");
        var whereIdx = sql.IndexOf("WHERE");
        var groupIdx = sql.IndexOf("GROUP BY");
        var havingIdx = sql.IndexOf("HAVING");

        Assert.True(fromIdx < whereIdx);
        Assert.True(whereIdx < groupIdx);
        Assert.True(groupIdx < havingIdx);
    }

    [Fact]
    public void SourceNameResolver_Replaces_FromAndJoin_Names()
    {
        var model = new KsqlQueryRoot()
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Select((o, c) => new { o.Id, c.Name })
            .Build();

        string Resolver(Type t) => t == typeof(Order) ? "ORDERS" : t == typeof(Customer) ? "CUSTOMERS" : t.Name;

        var sql = KsqlCreateStatementBuilder.Build("view", model, null, null, Resolver);
        Assert.Contains("FROM ORDERS", sql);
        Assert.Contains("JOIN CUSTOMERS", sql);
    }
}
