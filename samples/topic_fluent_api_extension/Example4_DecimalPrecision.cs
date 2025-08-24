using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;

namespace Samples.TopicFluentApiExtension;

// DecimalPrecision 属性を Fluent API に置き換える最小例
public static class Example4_DecimalPrecision
{
    [KsqlTopic("payments")]
    private class Payment
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
        [KsqlDecimal(18, 2)]
        public decimal Amount { get; set; }
    }

    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Payment>()
            .AsTable("payments");
    }
}
