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


[Collection("DataRoundTrip")]
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
        protected override bool SkipSchemaRegistration => true;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendAndReceive_CompositeKeyPoco()
    {
        try { await EnvCompositeKeyPocoTests.ResetAsync(); } catch { }
        try { await EnvCompositeKeyPocoTests.SetupAsync(); } catch { }
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
        options.Entities.Add(new EntityConfiguration { Entity = nameof(Order), EnableCache = true, SourceTopic = "orders_compkey" });
        options.Topics.Add("orders_compkey", new Configuration.Messaging.TopicSection {
            Consumer = new Configuration.Messaging.ConsumerSection { AutoOffsetReset = "Earliest", GroupId = Guid.NewGuid().ToString() },
            Creation = new Kafka.Ksql.Linq.Configuration.Messaging.TopicCreationSection { NumPartitions = 1, ReplicationFactor = 1 }
        });

        // Ensure topic exists and ksqlDB is ready BEFORE creating context (which performs schema registration + DDL)
        using (var preAdmin = new Confluent.Kafka.AdminClientBuilder(new Confluent.Kafka.AdminClientConfig { BootstrapServers = EnvCompositeKeyPocoTests.KafkaBootstrapServers }).Build())
        {
            try { await preAdmin.CreateTopicsAsync(new[] { new Confluent.Kafka.Admin.TopicSpecification { Name = "orders_compkey", NumPartitions = 1, ReplicationFactor = 1 } }); } catch { }
        }
        using (var admin = new Confluent.Kafka.AdminClientBuilder(new Confluent.Kafka.AdminClientConfig { BootstrapServers = EnvCompositeKeyPocoTests.KafkaBootstrapServers }).Build())
        {
            try { await admin.DeleteTopicsAsync(new[] { "orders_compkey" }); } catch { }
            try { await admin.CreateTopicsAsync(new[] { new Confluent.Kafka.Admin.TopicSpecification { Name = "orders_compkey", NumPartitions = 1, ReplicationFactor = 1 } }); } catch { }
            await PhysicalTestEnv.TopicHelpers.WaitForTopicReady(admin, "orders_compkey", 1, 1, TimeSpan.FromSeconds(10));
        }
        // Extra guard: wait for ksqlDB /info and apply a short grace
        await PhysicalTestEnv.KsqlHelpers.WaitForKsqlReadyAsync(EnvCompositeKeyPocoTests.KsqlDbUrl, TimeSpan.FromSeconds(120), graceMs: 1000);

        // Create context with retries to avoid transient initialization hiccups
        var ctx = await PhysicalTestEnv.KsqlHelpers.CreateContextWithRetryAsync(() => new OrderContext(options, loggerFactory), retries: 3, delayMs: 1000);
        await using var _ = ctx;

        // ksqlDB metadata readiness check is skipped in this test path; topic readiness is ensured above
        await Task.Delay(500);

        await ctx.Orders.AddAsync(new Order
        {
            OrderId = 1,
            UserId = 2,
            ProductId = 3,
            Quantity = 4
        });
        // Poll ToListAsync until data is observed or timeout
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(20);
        var list = await ctx.Orders.ToListAsync();
        while (list.Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500);
            list = await ctx.Orders.ToListAsync();
        }
        // Exclude priming dummy (composite key defaults)
        var filtered = list.FindAll(o => !(o.OrderId == 0 && o.UserId == 0));
        Assert.True(filtered.Count == 1, $"Expected 1 record excluding dummy, got {filtered.Count}");

        // Verify ForEachAsync can run briefly without throwing (cancel after 1s)
        using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(1)))
        {
            await ctx.Orders.ForEachAsync(_ => Task.CompletedTask, cancellationToken: cts.Token);
        }

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

    internal static async Task ResetAsync()
    {
        try { PhysicalTestEnv.Cleanup.DeleteLocalRocksDbState(); } catch { }
        var topics = new[] { "orders_compkey" };
        try { await PhysicalTestEnv.Cleanup.DeleteSubjectsAsync(SchemaRegistryUrl, topics); } catch { }
        try { await PhysicalTestEnv.Cleanup.DeleteTopicsAsync(KafkaBootstrapServers, topics); } catch { }
    }
    internal static async Task SetupAsync()
    {
        await PhysicalTestEnv.Health.WaitForKafkaAsync(KafkaBootstrapServers, TimeSpan.FromSeconds(120));
        await PhysicalTestEnv.Health.WaitForHttpOkAsync($"{SchemaRegistryUrl}/subjects", TimeSpan.FromSeconds(120));
        await PhysicalTestEnv.Health.WaitForHttpOkAsync($"{KsqlDbUrl}/info", TimeSpan.FromSeconds(120));
    }

    private class BasicContext : KsqlContext
    {
        public BasicContext(KsqlDslOptions options) : base(options) { }
        protected override bool SkipSchemaRegistration => true;
        protected override IEntitySet<T> CreateEntitySet<T>(EntityModel entityModel) => throw new NotImplementedException();
        protected override void OnModelCreating(IModelBuilder modelBuilder) { }
    }
}
