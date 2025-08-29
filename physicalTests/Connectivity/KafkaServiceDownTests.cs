using Kafka.Ksql.Linq.Core.Modeling;
using Confluent.Kafka;
using System;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using Xunit;

using Kafka.Ksql.Linq.Core.Abstractions;
using Xunit.Sdk;
namespace Kafka.Ksql.Linq.Tests.Integration;


public class KafkaServiceDownTests
{
    [KsqlTopic("orders")]
    public class Order
    {
        public int Id { get; set; }
        public double Amount { get; set; }
    }

    public class OrderContext : KsqlContext
    {
        public OrderContext() : base(new KsqlDslOptions()) { }
        public OrderContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
            => modelBuilder.Entity<Order>();
        protected override bool SkipSchemaRegistration => true;
    }

    private static KsqlDslOptions CreateOptions() => new()
    {
        Common = new CommonSection { BootstrapServers = EnvKafkaServiceDownTests.KafkaBootstrapServers },
        SchemaRegistry = new SchemaRegistrySection { Url = EnvKafkaServiceDownTests.SchemaRegistryUrl }
    };

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddAsync_ShouldThrow_WhenKafkaIsDown()
    {

        try
        {
            await EnvKafkaServiceDownTests.ResetAsync();

        }
        catch (Exception)
        {

        }
        await DockerHelper.StopServiceAsync("kafka");

        await using var ctx = new OrderContext(CreateOptions());
        var msg = new Order { Id = 1, Amount = 100 };

        var ex = await Assert.ThrowsAsync<KafkaException>(() =>
            ctx.Set<Order>().AddAsync(msg));
        Assert.Contains("refused", ex.Message, StringComparison.OrdinalIgnoreCase);

        await DockerHelper.StartServiceAsync("kafka");
        await EnvKafkaServiceDownTests.SetupAsync();

        await ctx.Set<Order>().AddAsync(new Order { Id = 2, Amount = 50 });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ForeachAsync_ShouldThrow_WhenKafkaIsDown()
    {


        try
        {
            await EnvKafkaServiceDownTests.ResetAsync();

        }
        catch (Exception)
        {

        }
        await DockerHelper.StopServiceAsync("kafka");

        await using var ctx = new OrderContext(CreateOptions());

        await Assert.ThrowsAsync<ConsumeException>(async () =>
        {
            await ctx.Set<Order>().ForEachAsync(_ => Task.CompletedTask, TimeSpan.FromSeconds(1));
        });

        await DockerHelper.StartServiceAsync("kafka");
        await EnvKafkaServiceDownTests.SetupAsync();
    }
}

// local environment helpers
public class EnvKafkaServiceDownTests
{
    internal const string SchemaRegistryUrl = "http://localhost:8081";
    internal const string KsqlDbUrl = "http://localhost:8088";
    internal const string KafkaBootstrapServers = "localhost:9092";
    internal const string SkipReason = "Skipped in CI due to missing ksqlDB instance or schema setup failure";

    internal static bool IsKsqlDbAvailable()
    {
        try
        {
            using var ctx = CreateContext();
            var r = ctx.ExecuteStatementAsync("SHOW TOPICS;").GetAwaiter().GetResult();
            return r.IsSuccess;
        }
        catch
        {
            return false;
        }
    }

    internal static KsqlContext CreateContext()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = SchemaRegistryUrl },
            KsqlDbUrl = KsqlDbUrl
        };
        return new BasicContext(options);
    }

    internal static Task ResetAsync() => Task.CompletedTask;
    internal static Task SetupAsync() => Task.CompletedTask;

    private class BasicContext : KsqlContext
    {
        public BasicContext(KsqlDslOptions options) : base(options) { }
        protected override bool SkipSchemaRegistration => true;
        protected override IEntitySet<T> CreateEntitySet<T>(EntityModel entityModel) => throw new NotImplementedException();
        protected override void OnModelCreating(IModelBuilder modelBuilder) { }
    }
}
