using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Dsl;

public class PropertyNameParsingTests
{
    private class Rate
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private class Schedule
    {
        public int Id { get; set; }
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime Day { get; set; }
    }

    [Fact]
    public void Tumbling_Extracts_TimeKey()
    {
        var q = Expression.Parameter(typeof(KsqlQueryable<Rate>), "q");
        var r = Expression.Parameter(typeof(Rate), "r");
        var timeLambda = Expression.Lambda(Expression.Property(r, nameof(Rate.Timestamp)), r);
        var method = typeof(KsqlQueryable<Rate>).GetMethods().First(m => m.Name == "Tumbling" && m.GetParameters().Length == 7);
        var call = Expression.Call(q, method,
            timeLambda,
            Expression.Constant(new[] { 1 }),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(int[])),
            Expression.Constant(null, typeof(DayOfWeek?)),
            Expression.Constant(null, typeof(TimeSpan?)));
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(call);
        Assert.Equal("Timestamp", visitor.Result.TimeKey);
    }

    [Fact]
    public void Tumbling_Sets_TimeKey_On_Model()
    {
        var model = new KsqlQueryable<Rate>()
            .Tumbling(r => r.Timestamp, minutes: new[] { 1 })
            .Build();
        Assert.Equal("Timestamp", model.TimeKey);
    }

    [Fact]
    public void TimeFrame_Extracts_DayKey()
    {
        var q = Expression.Parameter(typeof(KsqlQueryable<Rate>), "q");
        var r = Expression.Parameter(typeof(Rate), "r");
        var s = Expression.Parameter(typeof(Schedule), "s");
        var predicate = Expression.Lambda(
            Expression.AndAlso(
                Expression.Equal(Expression.Property(r, nameof(Rate.Id)), Expression.Property(s, nameof(Schedule.Id))),
                Expression.AndAlso(
                    Expression.LessThanOrEqual(Expression.Property(s, nameof(Schedule.Open)), Expression.Property(r, nameof(Rate.Timestamp))),
                    Expression.LessThan(Expression.Property(r, nameof(Rate.Timestamp)), Expression.Property(s, nameof(Schedule.Close))))),
            r, s);
        var dayLambda = Expression.Lambda(
            Expression.Convert(Expression.Property(s, nameof(Schedule.Day)), typeof(object)), s);
        var method = typeof(KsqlQueryable<Rate>).GetMethods().First(m => m.Name == "TimeFrame" && m.GetParameters().Length == 2);
        var generic = method.MakeGenericMethod(typeof(Schedule));
        var call = Expression.Call(q, generic, predicate, dayLambda);
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(call);
        Assert.Equal("Day", visitor.Result.BasedOnDayKey);
    }
}

