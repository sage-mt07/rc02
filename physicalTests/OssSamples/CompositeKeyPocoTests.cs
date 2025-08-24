using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Entities.Samples.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;


public class CompositeKeyPocoTests
{
    public class OrderContext : KsqlContext
    {
        public EventSet<Order> Orders { get; set; }
        public OrderContext() : base(new KsqlDslOptions()) { }
        public OrderContext(KsqlDslOptions options,ILoggerFactory loggerFactory) : base(options, loggerFactory) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
           // modelBuilder.Entity<Order>();
        }
    }

    [Fact(Skip = "")]
    [Trait("Category", "Integration")]
    public async Task SendAndReceive_CompositeKeyPoco()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Trace)  // ここで最低ログレベル指定
                .AddFilter("Streamiz.Kafka.Net", LogLevel.Trace)
                .AddConsole();
        });
        //await Env.ResetAsync();

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvCompositeKeyPocoTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvCompositeKeyPocoTests.SchemaRegistryUrl }
             
        };
        options.Topics.Add("orders", new Configuration.Messaging.TopicSection { Consumer = new Configuration.Messaging.ConsumerSection { AutoOffsetReset = "Earliest", GroupId = Guid.NewGuid().ToString() } });

        await using var ctx = new OrderContext(options, loggerFactory);

        await ctx.Orders.AddAsync(new Order
        {
            OrderId = 1,
            UserId = 2,
            ProductId = 3,
            Quantity = 4
        });
        await Task.Delay(5000);
        var list = await ctx.Orders.ToListAsync();
        Assert.Single(list);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.Orders.ForEachAsync(o => { return Task.CompletedTask; }, TimeSpan.FromSeconds(1)));

        await ctx.DisposeAsync();
    }
}

// local environment helpers
public class EnvCompositeKeyPocoTests
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
