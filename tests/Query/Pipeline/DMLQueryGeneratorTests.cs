using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Pipeline;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using Xunit;
namespace Kafka.Ksql.Linq.Tests.Query.Pipeline;

public class DMLQueryGeneratorTests
{
    private static T ExecuteInScope<T>(Func<T> func)
    {
        using (ModelCreatingScope.Enter())
        {
            return func();
        }
    }
    [Fact]
    public void GenerateSelectAll_WithPushQuery_AppendsEmitChanges()
    {
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateSelectAll("s1", isPullQuery: false));
        Assert.Equal("SELECT * FROM s1 EMIT CHANGES;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);

    }

    [Fact]
    public void GenerateSelectWithCondition_Basic()
    {
        Expression<Func<TestEntity, bool>> expr = e => e.Id == 1;
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateSelectWithCondition("s1", expr.Body, false));
        Assert.Equal("SELECT * FROM s1 WHERE (Id = 1) EMIT CHANGES;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateSelectAll_TableQuery_NoEmitChanges()
    {
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateSelectAll("t_orders", true, true));
        Assert.Equal("SELECT * FROM t_orders;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateSelectWithCondition_TableQuery_NoEmitChanges()
    {
        Expression<Func<TestEntity, bool>> expr = e => e.Id == 1;
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateSelectWithCondition("t_orders", expr.Body, true, true));
        Assert.Equal("SELECT * FROM t_orders WHERE (Id = 1);", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateCountQuery_ReturnsExpected()
    {
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateCountQuery("t1"));
        Assert.Equal("SELECT COUNT(*) FROM t1;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateAggregateQuery_Basic()
    {
        Expression<Func<TestEntity, object>> expr = e => new { Sum = e.Id };
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateAggregateQuery("t1", expr.Body));
        Assert.Contains("FROM t1", query);
        Assert.StartsWith("SELECT", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateAggregateQuery_LatestByOffset()
    {
        Expression<Func<IGrouping<int, TestEntity>, object>> expr = g => new { Last = g.LatestByOffset(x => x.Id) };
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateAggregateQuery("t1", expr.Body));
        Assert.Equal("SELECT LATEST_BY_OFFSET(Id) AS Last FROM t1;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateAggregateQuery_EarliestByOffset()
    {
        Expression<Func<IGrouping<int, TestEntity>, object>> expr = g => new { First = g.EarliestByOffset(x => x.Id) };
        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateAggregateQuery("t1", expr.Body));
        Assert.Equal("SELECT EARLIEST_BY_OFFSET(Id) AS First FROM t1;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_FullClauseCombination()
    {
        IQueryable<TestEntity> src = new List<TestEntity>().AsQueryable();
        var expr = src
            .Where(e => e.IsActive)
            .GroupBy(e => e.Type)
            .Having(g => g.Count() > 1)
            .Select(g => new { g.Key, Count = g.Count() })
            .OrderBy(x => x.Key);

        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateLinqQuery("s1", expr.Expression, false));

        Assert.Contains("SELECT Type", query);
        Assert.Contains("COUNT(*) AS Count", query);
        Assert.Contains("FROM s1", query);
        Assert.Contains("WHERE (IsActive = true)", query);
        Assert.Contains("GROUP BY Type", query);
        Assert.Contains("HAVING (COUNT(*) > 1)", query);
        Assert.Contains("ORDER BY", query);
        Assert.EndsWith("EMIT CHANGES;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    private class Order
    {
        public int CustomerId { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
        public bool IsHighPriority { get; set; }
    }

    private class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class OrderWithCount
    {
        public int CustomerId { get; set; }
        public double Amount { get; set; }
        public int Count { get; set; }
    }

    [Fact]
    public void GenerateLinqQuery_GroupBySelectHaving_ComplexCondition()
    {
        IQueryable<Order> src = new List<Order>().AsQueryable();

        var expr = src
            .GroupBy(o => o.CustomerId)
            .Having(g => g.Count() > 10 && g.Sum(x => x.Amount) < 5000)
            .Select(g => new { g.Key, OrderCount = g.Count(), TotalAmount = g.Sum(x => x.Amount) });

        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateLinqQuery("Orders", expr.Expression, false));

        Assert.Contains("SELECT CustomerId", query);
        Assert.Contains("COUNT(*) AS OrderCount", query);
        Assert.Contains("SUM(Amount) AS TotalAmount", query);
        Assert.Contains("FROM Orders", query);
        Assert.Contains("GROUP BY CustomerId", query);
        Assert.Contains("HAVING ((COUNT(*) > 10) AND (SUM(Amount) < 5000))", query);
        Assert.EndsWith("EMIT CHANGES;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateLINQQuery_JoinGroupByHavingCondition_ReturnsExpectedQuery()
    {
        IQueryable<Order> orders = new List<Order>().AsQueryable();
        IQueryable<Customer> customers = new List<Customer>().AsQueryable();

        var expr = orders
            .Join(customers, o => o.CustomerId, c => c.Id, (o, c) => new { o, c })
            .GroupBy(x => x.o.CustomerId)
            .Having(g => g.Count() > 2 && g.Sum(x => x.o.Amount) < 10000)
            .Select(g => new
            {
                g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.o.Amount)
            });

        var generator = new DMLQueryGenerator();
        var query = ExecuteInScope(() => generator.GenerateLinqQuery("Orders", expr.Expression, false));

        Assert.Contains("JOIN", query);
        Assert.Contains("GROUP BY CustomerId", query);
        Assert.Contains("HAVING ((COUNT(*) > 2) AND (SUM(Amount) < 10000))", query);
        Assert.Contains("COUNT(*) AS OrderCount", query);
        Assert.Contains("SUM(Amount) AS TotalAmount", query);
        Assert.EndsWith("EMIT CHANGES;", query);
        File.AppendAllText("generated_queries.txt", query + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_JoinGroupByHavingCondition_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();
        var customers = new List<Customer>().AsQueryable();

        var query =
            (from o in orders
             join c in customers on o.CustomerId equals c.Id
             group o by o.CustomerId into g
             select g)
            .Having(g => g.Count() > 2 && g.Sum(x => x.Amount) < 10000)
            .Select(g => new
            {
                g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("joined", query.Expression, false));

        Assert.Contains("JOIN", result);
        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("COUNT(*)", result);
        Assert.Contains("SUM(", result);
        Assert.Contains("HAVING ((COUNT(*) > 2) AND (SUM(Amount) < 10000))", result);
        Assert.EndsWith("EMIT CHANGES;", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByHavingWithMultipleAggregates_ReturnsExpectedQuery()
    {
        var src = new List<Order>().AsQueryable();

        var query = src
            .GroupBy(o => o.CustomerId)
            .Having(g => g.Average(x => x.Amount) > 100 && g.Sum(x => x.Amount) < 1000)
            .Select(g => new
            {
                g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                AvgAmount = g.Average(x => x.Amount),
                TotalSmall = g.Sum(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("multiagg", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("AVG(", result);
        Assert.DoesNotContain("MAX(", result);
        Assert.Contains("COUNT(*)", result);
        Assert.Contains("SUM(", result);
        Assert.Contains("HAVING ((AVG(Amount) > 100) AND (SUM(Amount) < 1000))", result);
        Assert.Contains("TotalSmall", result);
        Assert.EndsWith("EMIT CHANGES;", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_JoinGroupByHavingCombination_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();
        var customers = new List<Customer>().AsQueryable();

        var query = orders
            .Join(
                customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { o, c }
            )
            .GroupBy(x => x.c.Name)
            .Having(g => g.Count() > 5 && g.Sum(x => x.o.Amount) > 1000)
            .Select(g => new
            {
                g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.o.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("joined", query.Expression, false));

        Assert.Contains("JOIN", result);
        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("COUNT(", result);
        Assert.Contains("SUM(", result);
        Assert.EndsWith("EMIT CHANGES;", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_MultiKeyGroupByWithHaving_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => new { o.CustomerId, o.Region })
            .Having(g => g.Sum(x => x.Amount) > 1000)
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Region,
                Total = g.Sum(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("CustomerId", result);
        Assert.Contains("Region", result);
        Assert.Contains("SUM", result);
        Assert.EndsWith("EMIT CHANGES;", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithConditionalSum_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount),
                HighPriorityTotal = g.Sum(o => o.IsHighPriority ? o.Amount : 0)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("CASE WHEN", result);
        Assert.Contains("SUM", result);
        Assert.Contains("HighPriorityTotal", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithAvgSum_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                AverageAmount = g.Average(o => o.Amount),
                TotalAmount = g.Sum(o => o.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("AVG", result);
        Assert.Contains("SUM", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByAnonymousKeyWithKeyProjection_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => new { o.CustomerId, o.Region })
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("CustomerId", result);
        Assert.Contains("Region", result);
        Assert.Contains("SUM", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupBySelectOrderBy_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount)
            })
            .OrderBy(x => x.Total);

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("SUM", result);
        Assert.Contains("ORDER BY", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupBySelectOrderByDescending_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount)
            })
            .OrderByDescending(x => x.Total); // descending sort

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("SUM", result);
        Assert.Contains("ORDER BY", result);
        Assert.Contains("DESC", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_OrderByThenByDescending_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => new { o.CustomerId, o.Region })
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Region,
                Total = g.Sum(o => o.Amount)
            })
            .OrderBy(x => x.CustomerId)            // ascending
            .ThenByDescending(x => x.Total);       // descending

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("ORDER BY", result);
        Assert.Contains("CustomerId", result);
        Assert.Contains("Total", result);
        Assert.Contains("DESC", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_MultiKeyGroupByMultipleAggregates_HavingComplexConditions_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => new { o.CustomerId, o.Region })
            .Having(g => (g.Sum(x => x.Amount) > 1000 && g.Count() > 10) || g.Average(x => x.Amount) > 150)
            .Select(g => new
            {
                g.Key.CustomerId,
                g.Key.Region,
                TotalAmount = g.Sum(x => x.Amount),
                OrderCount = g.Count(),
                AverageAmount = g.Average(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("SUM", result);
        Assert.Contains("COUNT", result);
        Assert.Contains("AVG", result);
        Assert.Contains("AND", result);
        Assert.Contains("OR", result);
        Assert.EndsWith("EMIT CHANGES;", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithCaseWhen_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount),
                Status = g.Sum(o => o.Amount) > 1000 ? "VIP" : "Regular"
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("SUM", result);
        Assert.Contains("CASE", result);
        Assert.Contains("WHEN", result);
        Assert.Contains("THEN", result);
        Assert.Contains("ELSE", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithComplexHavingConditions_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Where(g =>
                (g.Sum(o => o.Amount) > 1000 && g.Count() > 5) ||
                g.Average(o => o.Amount) > 500)
            .Select(g => new
            {
                g.Key,
                Total = g.Sum(o => o.Amount),
                Count = g.Count(),
                Avg = g.Average(o => o.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("AND", result);
        Assert.Contains("OR", result);
        Assert.Contains("(", result);
        Assert.Contains(")", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithComplexOrHavingCondition_ReturnsExpectedQuery()
    {
        var orders = new List<OrderWithCount>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Where(g => g.Sum(x => x.Amount) > 1000 || g.Sum(x => x.Count) > 5)
            .Select(g => new
            {
                g.Key,
                TotalAmount = g.Sum(x => x.Amount),
                TotalCount = g.Sum(x => x.Count)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY CustomerId", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("SUM", result);
        Assert.Contains(" OR ", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_WhereNotInClause_ReturnsExpectedQuery()
    {
        var excludedRegions = new[] { "CN", "RU" };
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .Where(o => !excludedRegions.Contains(o.Region))
            .Select(o => new
            {
                o.CustomerId,
                o.Region,
                o.Amount
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("WHERE", result);
        Assert.Contains("NOT IN", result);
        Assert.Contains("'CN'", result);
        Assert.Contains("'RU'", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    private class NullableOrder
    {
        public int? CustomerId { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
    }

    private class NullableKeyOrder
    {
        public int? CustomerId { get; set; }
        public double Amount { get; set; }
    }

    [Fact]
    public void GenerateLinqQuery_WhereIsNullClause_ReturnsExpectedQuery()
    {
        var orders = new List<NullableOrder>().AsQueryable();

        var query = orders
            .Where(o => o.CustomerId == null)
            .Select(o => new
            {
                o.Region,
                o.Amount
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("WHERE", result);
        Assert.Contains("IS NULL", result);
        Assert.Contains("CustomerId", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_WhereIsNotNullClause_ReturnsExpectedQuery()
    {
        var orders = new List<NullableOrder>().AsQueryable();

        var query = orders
            .Where(o => o.CustomerId != null)
            .Select(o => new
            {
                o.Region,
                o.Amount
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("WHERE", result);
        Assert.Contains("IS NOT NULL", result);
        Assert.Contains("CustomerId", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByNullableKey_WithWhereNotNull_ProducesCorrectQuery()
    {
        var orders = new List<NullableKeyOrder>().AsQueryable();

        var query = orders
            .Where(o => o.CustomerId != null)
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Total = g.Sum(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("WHERE CustomerId IS NOT NULL", result);
        Assert.Contains("GROUP BY CustomerId", result);
        Assert.Contains("SUM", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByWithExpressionKey_ReturnsExpectedQuery()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.Region.ToUpper())
            .Where(g => g.Sum(x => x.Amount) > 500)
            .Select(g => new
            {
                RegionUpper = g.Key,
                TotalAmount = g.Sum(x => x.Amount)
            });

        var generator = new DMLQueryGenerator();
        var result = ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false));

        Assert.Contains("GROUP BY", result);
        Assert.Contains("UPPER", result);
        Assert.Contains("HAVING", result);
        Assert.Contains("SUM", result);
        Assert.Contains("EMIT CHANGES", result);
        File.AppendAllText("generated_queries.txt", result + Environment.NewLine);
    }

    [Fact]
    public void GenerateLinqQuery_NestedAggregate_ThrowsNotSupportedException()
    {
        var orders = new List<Order>().AsQueryable();

        var query = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                AvgTotal = g.Average(x => g.Sum(y => y.Amount))
            });

        var generator = new DMLQueryGenerator();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, false)));

        Assert.Contains("Nested aggregate functions are not supported", ex.Message);
    }

    [Fact]
    public void GenerateSelectAll_OutsideScope_Throws()
    {
        var generator = new DMLQueryGenerator();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            generator.GenerateSelectAll("s1"));

        Assert.Contains("Where/GroupBy/Select", ex.Message);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByPullQuery_Throws()
    {
        var src = new List<Order>().AsQueryable();
        var query = src
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() });

        var generator = new DMLQueryGenerator();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, true)));

        Assert.Contains("GROUP BY is not supported in pull or table queries", ex.Message);
    }

    [Fact]
    public void GenerateLinqQuery_GroupByTableQuery_Throws()
    {
        var src = new List<Order>().AsQueryable();
        var query = src
            .GroupBy(o => o.CustomerId)
            .Select(g => new { g.Key, Count = g.Count() });

        var generator = new DMLQueryGenerator();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ExecuteInScope(() => generator.GenerateLinqQuery("orders", query.Expression, true, true)));

        Assert.Contains("GROUP BY is not supported in pull or table queries", ex.Message);
    }
}
