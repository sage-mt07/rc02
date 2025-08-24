using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;

namespace Samples.TopicFluentApiExtension;

// シンプルなトピック名とパーティション設定の例
public static class Example1_Basic
{
    [KsqlTopic("orders", PartitionCount = 3)]
    private class Order
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
    }

    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Order>()
            .AsTable("orders");
    }
}
