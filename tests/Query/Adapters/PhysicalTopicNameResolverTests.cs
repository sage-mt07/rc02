using System.Collections.Generic;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Infrastructure.KsqlDb;
using Kafka.Ksql.Linq.Query.Adapters;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Adapters
{
    public class PhysicalTopicNameResolverTests
    {
        private class StubDict : IDictionaryKvClient
        {
            public Task<string?> GetAsync(string key)
            {
                if (key == "topic/sc.kksl.orders/order_customer_join_v2/pub/kafka_topic")
                    return Task.FromResult<string?>("sc.kksl.orders.order_customer_join_v2.pub");
                return Task.FromResult<string?>(null);
            }
            public Task<Dictionary<string, string>> GetByPrefixAsync(string prefix) => Task.FromResult(new Dictionary<string, string>());
            public Task UpsertAsync(string key, string value) => Task.CompletedTask;
        }

        [Fact]
        public async Task UsesDictionaryValue()
        {
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            var name = await TopicNameResolver.ResolvePhysicalAsync(model, "pub", new StubDict());
            Assert.Equal("sc.kksl.orders.order_customer_join_v2.pub", name);
        }

        [Fact]
        public async Task DefaultsWhenMissing()
        {
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            var name = await TopicNameResolver.ResolvePhysicalAsync(model, "int", new StubDict());
            Assert.Equal("sc.kksl.orders.order_customer_join_v2.int", name);
        }
    }
}

namespace Sc.Kksl.Orders
{
    class OrderCustomerJoinV2 { }
}
