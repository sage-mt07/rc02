using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Attributes;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.ModelBuilderTests;

public class TopicAttributeTests
{
    [KsqlTopic("orders", PartitionCount = 3, ReplicationFactor = 2)]
    private class Order
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
    }

    [Fact]
    public void Attribute_ConfiguresTopicSettings()
    {
        var builder = new ModelBuilder();
        builder.Entity<Order>()
            .AsTable();

        var model = builder.GetEntityModel<Order>();
        Assert.NotNull(model);
        Assert.Equal("orders", model.TopicName);
    }
}
