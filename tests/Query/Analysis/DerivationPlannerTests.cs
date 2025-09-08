using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Dsl;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class DerivationPlannerTests
{
    [KsqlTopic("bar")]
    private class Source
    {
        public int Id { get; set; }
    }

    [KsqlTopic("bar")]
    private class SourceOhlc
    {
        public int Id { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double KsqlTimeFrameClose { get; set; }
    }

    private static TumblingQao Create(Timeframe tf) => new()
    {
        TimeKey = "Timestamp",
        Windows = new[] { tf },
        Keys = new[] { "Id" },
        Projection = new[] { "Id" },
        PocoShape = new[] { new ColumnShape("Id", typeof(int), false) },
        BasedOn = new BasedOnSpec(new[] { "Id" }, string.Empty, "KsqlTimeFrameClose", string.Empty),
        WeekAnchor = DayOfWeek.Monday
    };

    [Fact]
    public void Plan_1m_Includes_All_Roles()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(1, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_1m_agg_final" && e.Role == Role.AggFinal);
        Assert.Contains(entities, e => e.Id == "bar_1m_live" && e.Role == Role.Live);
        Assert.Contains(entities, e => e.Id == "bar_1m_final" && e.Role == Role.Final);
        Assert.Contains(entities, e => e.Id == "bar_prev_1m" && e.Role == Role.Prev1m);
        Assert.Contains(entities, e => e.Id == "bar_hb_1m" && e.Role == Role.Hb);
    }

    [Fact]
    public void Plan_5m_Excludes_Prev_And_Hb()
    {
        var model = new EntityModel { EntityType = typeof(Source) };
        var entities = DerivationPlanner.Plan(Create(new Timeframe(5, "m")), model);

        Assert.Contains(entities, e => e.Id == "bar_5m_agg_final" && e.Role == Role.AggFinal);
        Assert.Contains(entities, e => e.Id == "bar_5m_live" && e.Role == Role.Live);
        Assert.Contains(entities, e => e.Id == "bar_5m_final" && e.Role == Role.Final);
        Assert.DoesNotContain(entities, e => e.Role == Role.Prev1m);
        Assert.DoesNotContain(entities, e => e.Role == Role.Hb);
    }

    [Fact]
    public void Prev1m_Static_Table_Single_Value()
    {
        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(1, "m") },
            Keys = new[] { "Id" },
            Projection = new[] { "Id", "Open", "High", "Low", "KsqlTimeFrameClose" },
            PocoShape = new[]
            {
                new ColumnShape("Id", typeof(int), false),
                new ColumnShape("Open", typeof(double), false),
                new ColumnShape("High", typeof(double), false),
                new ColumnShape("Low", typeof(double), false),
                new ColumnShape("KsqlTimeFrameClose", typeof(double), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Id" }, string.Empty, "KsqlTimeFrameClose", string.Empty),
            WeekAnchor = DayOfWeek.Monday
        };
        var baseModel = new EntityModel { EntityType = typeof(SourceOhlc) };
        var entities = DerivationPlanner.Plan(qao, baseModel);
        var models = EntityModelAdapter.Adapt(entities);
        var prev = models.Single(m => (string)m.AdditionalSettings["role"] == Role.Prev1m.ToString());
        var close = qao.BasedOn.CloseProp;
        Assert.Equal(new[] { close }, (string[])prev.AdditionalSettings["projection"]);
        var method = typeof(DerivedTumblingPipeline).GetMethod("BuildDdlAndRegister", BindingFlags.NonPublic | BindingFlags.Static);
        var qm = new KsqlQueryModel { SourceTypes = new[] { typeof(SourceOhlc) } };
        var res = ((string ddl, Type _, string? ns))method!.Invoke(null, new object[] { "bar", qm, prev, Role.Prev1m, (Func<string, Type>)(_ => typeof(object)) })!;
        Assert.StartsWith("CREATE TABLE bar_prev_1m", res.ddl);
        Assert.DoesNotContain("AS SELECT", res.ddl);
        Assert.Contains(close, res.ddl);
    }
}
