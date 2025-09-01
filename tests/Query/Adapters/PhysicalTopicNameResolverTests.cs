using System;
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
            public Func<string, Task<string?>> Handler { get; set; } = _ => Task.FromResult<string?>(null);
            public Task<string?> GetAsync(string key) => Handler(key);
            public Task<Dictionary<string, string>> GetByPrefixAsync(string prefix) => Task.FromResult(new Dictionary<string, string>());
            public Task UpsertAsync(string key, string value) => Task.CompletedTask;
        }

        [Fact]
        public async Task UsesDictionaryValue()
        {
            var dict = new StubDict { Handler = key => Task.FromResult<string?>("sc.kksl.orders.order_customer_join_v2") };
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            var name = await TopicNameResolver.ResolvePhysicalAsync(model, dict);
            Assert.Equal("sc.kksl.orders.order_customer_join_v2", name);
        }

        [Fact]
        public async Task ThrowsWhenMissing()
        {
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            await Assert.ThrowsAsync<InvalidOperationException>(() => TopicNameResolver.ResolvePhysicalAsync(model, new StubDict()));
        }

        [Fact]
        public async Task ThrowsWhenEmpty()
        {
            var dict = new StubDict { Handler = _ => Task.FromResult<string?>("") };
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            await Assert.ThrowsAsync<InvalidOperationException>(() => TopicNameResolver.ResolvePhysicalAsync(model, dict));
        }

        [Fact]
        public async Task PropagatesDictionaryError()
        {
            var dict = new StubDict { Handler = _ => Task.FromException<string?>(new InvalidOperationException("boom")) };
            var model = new EntityModel { EntityType = typeof(Sc.Kksl.Orders.OrderCustomerJoinV2) };
            await Assert.ThrowsAsync<InvalidOperationException>(() => TopicNameResolver.ResolvePhysicalAsync(model, dict));
        }
    }
}

namespace Sc.Kksl.Orders
{
    class OrderCustomerJoinV2 { }
}
