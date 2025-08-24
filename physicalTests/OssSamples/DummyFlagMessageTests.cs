using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Configuration.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

#nullable enable
namespace Kafka.Ksql.Linq.Tests.Integration;


public class DummyFlagMessageTests
{

    [KsqlTopic("orders")]
    public class OrderValue
    {
        public int CustomerId { get; set; }
        public int Id { get; set; }
        public string? Region { get; set; }
        public double Amount { get; set; }
        public bool IsHighPriority { get; set; }
        public int Count { get; set; }
    }

    public class DummyContext : KsqlContext
    {
        public EventSet<OrderValue> OrderValues { get; set; } = default!;
        public DummyContext() : base(new KsqlDslOptions()) { }
        public DummyContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
          //  modelBuilder.Entity<OrderValue>();
        }
    }

    // KafkaProducer が is_dummy ヘッダーを追加し EventSet で取得できるか確認
    [Fact]
    public async Task SendAsync_AddsDummyFlagHeader()
    {


        //await Env.ResetAsync();

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvDummyFlagMessageTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvDummyFlagMessageTests.SchemaRegistryUrl },
            Topics = new Dictionary<string, TopicSection>
            {
                ["orders"] = new TopicSection
                {
                    Consumer = new ConsumerSection { AutoOffsetReset = "Earliest" }
                }
            }
        };

        await using var ctx = new DummyContext(options);

        var headers = new Dictionary<string, string> { ["is_dummy"] = "true" };

        await ctx.OrderValues.AddAsync(new OrderValue
        {
            CustomerId = 1,
            Id = 1,
            Region = "west",
            Amount = 10d,
            IsHighPriority = false,
            Count = 1
        }, headers);

        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.OrderValues.ToListAsync());

        // 通常の ForEachAsync では is_dummy ヘッダー付きメッセージがスキップされる
        var consumed = new List<OrderValue>();
        await ctx.OrderValues.ForEachAsync(o =>
        {
            consumed.Add(o);
            return Task.CompletedTask;
        }, TimeSpan.FromSeconds(100));
        Assert.Empty(consumed);

        // KafkaMessageContext を受け取るオーバーロードではヘッダーを確認可能
        var contexts = new Dictionary<string, string>();
           
        await ctx.OrderValues.ForEachAsync((o, c, m) =>
        {
            foreach(var key in c.Keys)
            {
                contexts.Add(key, c[key]);
            }
            return Task.CompletedTask;
        }, TimeSpan.FromSeconds(1));
        Assert.Single(contexts);
        Assert.Equal("true", contexts["is_dummy"].ToString());

        await ctx.DisposeAsync();
    }
}

// local environment helpers
public class EnvDummyFlagMessageTests
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
