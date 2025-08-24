using System;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class TumblingWeekTests
{
    private class Rate
    {
        public DateTime Timestamp { get; set; }
    }

    [Fact]
    public void Tumbling_Adds_1wk_Window()
    {
        var model = new KsqlQueryable<Rate>()
            .Tumbling(r => r.Timestamp, week: DayOfWeek.Monday)
            .Build();
        Assert.Contains("1wk", model.Windows);
        Assert.Equal(DayOfWeek.Monday, model.WeekAnchor);
    }

    [Fact]
    public void QueryModel_Contains_1wk_WhenDeclared()
    {
        var q = Expression.Parameter(typeof(KsqlQueryable<Rate>), "q");
        var r = Expression.Parameter(typeof(Rate), "r");
        var timeLambda = Expression.Lambda(Expression.Property(r, nameof(Rate.Timestamp)), r);
        var method = typeof(KsqlQueryable<Rate>).GetMethods()
            .First(m => m.Name == "Tumbling" && m.GetParameters().Length == 7);
        var call = Expression.Call(q, method,
            timeLambda,
            Expression.Constant(new[] { 1 }),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek?)),
            Expression.Constant(null, typeof(TimeSpan?))
        );
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(call);
        Assert.Contains("1wk", visitor.Result.Windows);
    }
}
