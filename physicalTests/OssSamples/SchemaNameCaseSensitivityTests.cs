using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;


[Collection("DataRoundTrip")]
public class SchemaNameCaseSensitivityTests
{

    [KsqlTopic("orders")]
    public class OrderCorrectCase
    {
        public int CustomerId { get; set; }
        public int Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
        public bool IsHighPriority { get; set; } = false;
        public int Count { get; set; }
    }

    public class OrderContext : KsqlContext
    {
        public EventSet<OrderCorrectCase> OrderCorrectCases { get; set; }
        public OrderContext() : base(new KsqlDslOptions()) { }
        public OrderContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
        //    modelBuilder.Entity<OrderCorrectCase>();
        }
    }

    // Schema Registry はフィールド名も含めスキーマとの完全一致を要求する。
    // 既存スキーマと一致するフィールド名・数を定義することで登録エラーを防ぐ。
    [Fact]
    [Trait("Category", "Integration")]
    public async Task LowercaseField_ShouldSucceed()
    {


        //try
        //{
        //    await Env.ResetAsync();
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"[Warning] ResetAsync failed: {ex}");
        //    throw new SkipException($"Test setup failed in ResetAsync: {ex.Message}");
        //}

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvSchemaNameCaseSensitivityTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvSchemaNameCaseSensitivityTests.SchemaRegistryUrl }
        };

        await using var ctx = new OrderContext(options);

        var headers = new Dictionary<string, string> { ["is_dummy"] = "true" };
        await ctx.OrderCorrectCases.AddAsync(new OrderCorrectCase
        {
            CustomerId = 1,
            Id = 1,
            Region = "east",
            Amount = 10d,
            IsHighPriority = false,
            Count = 1
        }, headers);


        var timeout = TimeSpan.FromSeconds(5);
        await ctx.WaitForEntityReadyAsync<OrderCorrectCase>(timeout);
    }
}

// local environment helpers
public class EnvSchemaNameCaseSensitivityTests
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
