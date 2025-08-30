using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;


[Collection("DDL")]
public class DummyFlagSchemaRecognitionTests
{

    [KsqlTopic("orders_dummyflag")]
    public class OrderValue
    {
        public int CustomerId { get; set; }
        public int Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
        public bool IsHighPriority { get; set; }
        public int Count { get; set; }
    }

    [KsqlTopic("customers")]
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [KsqlTopic("events")]
    public class EventLog
    {
        public int Level { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    [KsqlTopic("orders_nullable")]
    public class NullableOrder
    {
        public int? CustomerId { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
    }

    [KsqlTopic("orders_nullable_key")]
    public class NullableKeyOrder
    {
        public int? CustomerId { get; set; }
        public double Amount { get; set; }
    }

    public class DummyContext : KsqlContext
    {
        public DummyContext() : base(new KsqlDslOptions()) { }
        public DummyContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderValue>();
            modelBuilder.Entity<Customer>();
            modelBuilder.Entity<EventLog>();
            modelBuilder.Entity<NullableOrder>();
            modelBuilder.Entity<NullableKeyOrder>();
        }
    }

    private async Task ProduceDummyRecordsAsync()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvDummyFlagSchemaRecognitionTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvDummyFlagSchemaRecognitionTests.SchemaRegistryUrl },
            SourceNameOverrides = new Dictionary<string, string>
            {
                [nameof(OrderValue)] = "ORDERS",
                [nameof(Customer)] = "CUSTOMERS",
                [nameof(EventLog)] = "EVENTS",
                [nameof(NullableOrder)] = "ORDERS_NULLABLE",
                [nameof(NullableKeyOrder)] = "ORDERS_NULLABLE_KEY"
            }
        };

        await using var ctx = new DummyContext(options);

        var headers = new Dictionary<string, string> { ["is_dummy"] = "true" };

        await ctx.Set<OrderValue>().AddAsync(new OrderValue
        {
            CustomerId = 1,
            Id = 1,
            Region = "east",
            Amount = 10d,
            IsHighPriority = false,
            Count = 1
        }, headers);
        await ctx.Set<Customer>().AddAsync(new Customer { Id = 1, Name = "alice" }, headers);
        await ctx.Set<EventLog>().AddAsync(new EventLog { Level = 1, Message = "init" }, headers);
        await ctx.Set<NullableOrder>().AddAsync(new NullableOrder { CustomerId = 1, Region = "east", Amount = 10d }, headers);
        await ctx.Set<NullableKeyOrder>().AddAsync(new NullableKeyOrder { CustomerId = 1, Amount = 10d }, headers);

        var timeout = TimeSpan.FromSeconds(5);
        await ctx.WaitForEntityReadyAsync<OrderValue>(timeout);
        await ctx.WaitForEntityReadyAsync<Customer>(timeout);
        await ctx.WaitForEntityReadyAsync<EventLog>(timeout);
        await ctx.WaitForEntityReadyAsync<NullableOrder>(timeout);
        await ctx.WaitForEntityReadyAsync<NullableKeyOrder>(timeout);

        await ctx.DisposeAsync();
    }

    // ダミーメッセージを送信しスキーマを登録後、各クエリが実行可能か確認
    [Fact]
    [Trait("Category", "Integration")]
    public async Task DummyMessages_EnableQueries()
    {

        try { await EnvDummyFlagSchemaRecognitionTests.ResetAsync(); } catch { }
        try { await EnvDummyFlagSchemaRecognitionTests.SetupAsync(); } catch { }

        await using (var ctx = EnvDummyFlagSchemaRecognitionTests.CreateContext())
        {
            foreach (var ddl in TestSchema.GenerateTableDdls())
            {
                // retry up to 3 times per DDL to avoid transient startup races
                var attempts = 0; Exception? last = null;
                while (attempts++ < 3)
                {
                    var r = await ctx.ExecuteStatementAsync(ddl);
                    if (r.IsSuccess) { last = null; break; }
                    last = new InvalidOperationException(r.Message);
                    await Task.Delay(1000);
                }
                if (last != null) throw last;
            }
        }

        await ProduceDummyRecordsAsync();

        var queries = new[]
        {
            "SELECT * FROM ORDERS EMIT CHANGES LIMIT 1;",
            "SELECT * FROM CUSTOMERS EMIT CHANGES LIMIT 1;",
            "SELECT COUNT(*) FROM EVENTS;",
            "SELECT REGION, COUNT(*) FROM ORDERS GROUP BY REGION EMIT CHANGES LIMIT 1;"
        };

        await using (var ctx = EnvDummyFlagSchemaRecognitionTests.CreateContext())
        {
            foreach (var q in queries)
            {
                var r = await ctx.ExecuteExplainAsync(q);
                Assert.True(r.IsSuccess, $"{q} failed: {r.Message}");
            }
        }
    }

}

// local environment helpers
public class EnvDummyFlagSchemaRecognitionTests
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
            KsqlDbUrl = KsqlDbUrl,
            SourceNameOverrides = new Dictionary<string, string>
            {
                [nameof(DummyFlagSchemaRecognitionTests.OrderValue)] = "ORDERS",
                [nameof(DummyFlagSchemaRecognitionTests.Customer)] = "CUSTOMERS",
                [nameof(DummyFlagSchemaRecognitionTests.EventLog)] = "EVENTS",
                [nameof(DummyFlagSchemaRecognitionTests.NullableOrder)] = "ORDERS_NULLABLE",
                [nameof(DummyFlagSchemaRecognitionTests.NullableKeyOrder)] = "ORDERS_NULLABLE_KEY"
            }
        };
        return new BasicContext(options);
    }

    internal static async Task ResetAsync()
    {
        // Best-effort cleanup for topics, subjects, and local RocksDB state
        try
        {
            PhysicalTestEnv.Cleanup.DeleteLocalRocksDbState();
        }
        catch { }

        try
        {
            await PhysicalTestEnv.Cleanup.DeleteSubjectsAsync(SchemaRegistryUrl, TestSchema.AllTopicNames);
        }
        catch { }

        try
        {
            await PhysicalTestEnv.Cleanup.DeleteTopicsAsync(KafkaBootstrapServers, TestSchema.AllTopicNames);
        }
        catch { }
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
