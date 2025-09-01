using Kafka.Ksql.Linq.Infrastructure.Naming;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Infrastructure.Naming;

public class SnakeCaseConverterTests
{
    [Theory]
    [InlineData("OrderCustomerJoin", "order_customer_join")]
    [InlineData("OrderCustomerJoinV2", "order_customer_join_v2")]
    [InlineData("IPAddress", "ip_address")]
    [InlineData("HTTP2Server", "http2_server")]
    public void Converts(string input, string expected)
    {
        Assert.Equal(expected, SnakeCaseConverter.ToSnakeCase(input));
    }

    [Fact]
    public void CultureInvariant()
    {
        var original = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            Assert.Equal("i", SnakeCaseConverter.ToSnakeCase("I"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
        }
    }
}
