using Kafka.Ksql.Linq.Infrastructure.Naming;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Infrastructure.Naming;

public class SnakeCaseConverterTests
{
    [Theory]
    [InlineData("OrderCustomerJoin", "order_customer_join")]
    [InlineData("OrderCustomerJoinV2", "order_customer_join_v2")]
    public void Converts(string input, string expected)
    {
        Assert.Equal(expected, SnakeCaseConverter.ToSnakeCase(input));
    }
}
