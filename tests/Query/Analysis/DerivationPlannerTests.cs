using Kafka.Ksql.Linq.Query.Analysis;
using System;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class DerivationPlannerTests
{
    [Fact]
    public void PlanReturnsDerivedEntities()
    {
        var qao = new TumblingQao
        {
            BaseTopicName = "bar",
            TimeKey = "Timestamp",
            Windows = new[] { new Timeframe(5, "m") },
            Keys = new[] { "Id" },
            Projection = new[] { "Id" },
            PocoShape = new[] { new ColumnShape("Id", typeof(int), false) },
            BasedOn = new BasedOnSpec(new[] { "Id" }, string.Empty, string.Empty, string.Empty),
            WeekAnchor = DayOfWeek.Monday
        };

        var entities = DerivationPlanner.Plan(qao);

        Assert.Equal(3, entities.Count);
        Assert.Contains(entities, e => e.Id == "bar_5m_final");
    }
}
