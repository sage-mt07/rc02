using Confluent.SchemaRegistry;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace Kafka.Ksql.Linq.Tests.Application;

public class MaterializationExtensionsTests
{
    [KsqlTopic("orders")]
    public class Order
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestContext : KsqlContext
    {
        public StubEntitySet<Order>? StubSet;
        private readonly ISchemaRegistryClient _client;
        public TestContext(ISchemaRegistryClient client) : base(new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = "b" },
            SchemaRegistry = new SchemaRegistrySection { Url = "dummy" }
        })
        {
            _client = client;
            var field = typeof(KsqlContext).GetField("_schemaRegistryClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(this, new Lazy<ISchemaRegistryClient>(() => _client));
        }
        protected override bool SkipSchemaRegistration => true;
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>();
        }
        protected override IEntitySet<T> CreateEntitySet<T>(EntityModel model)
        {
            if (typeof(T) == typeof(Order))
            {
                StubSet = new StubEntitySet<Order>();
                return (IEntitySet<T>)StubSet;
            }
            return base.CreateEntitySet<T>(model);
        }
    }

    [Fact]
    public async Task SendsDummyOnlyOnFirstRegistration()
    {
        var client = new FakeSchemaRegistryClient();
        await using var ctx = new TestContext(client);
        await ctx.EnsureMaterializedIfSchemaIsNewAsync<Order>();
        Assert.Null(ctx.StubSet);

        await ctx.EnsureMaterializedIfSchemaIsNewAsync<Order>();
        Assert.Null(ctx.StubSet);
    }
}
