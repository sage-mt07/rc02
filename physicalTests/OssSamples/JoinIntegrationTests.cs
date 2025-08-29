using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Builders;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

public class JoinIntegrationTests
{
    [KsqlTopic("orders")]
    public class OrderValue
    {
        public int CustomerId { get; set; }
        public int Id { get; set; }
        public string Region { get; set; } = string.Empty;
        public double Amount { get; set; }
    }

    [KsqlTopic("customers")]
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }


    public class JoinContext : KsqlContext
    {
        public JoinContext() : base(new KsqlDslOptions()) { }
        public JoinContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {

            modelBuilder.Entity<OrderValue>();
            modelBuilder.Entity<Customer>();

        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task TwoTableJoin_Query_ShouldBeValid()
    {


        try
        {
            await EnvJoinIntegrationTests.ResetAsync();
        }
        catch (Exception)
        {
        }

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvJoinIntegrationTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvJoinIntegrationTests.SchemaRegistryUrl }
        };

        await using var ctx = new JoinContext(options);

        var model = new KsqlQueryRoot()
            .From<OrderValue>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Select((o, c) => new { o.CustomerId, c.Name, o.Amount })
            .Build();

        var ksql = KsqlCreateStatementBuilder
            .Build("orders_customers", model)
            // Replace type names only at safe boundaries to avoid corrupting property names
            .Replace(" FROM OrderValue ", " FROM orders ")
            .Replace(" JOIN Customer ", " JOIN customers ");

        var response = await ctx.ExecuteExplainAsync(ksql);
        Assert.True(response.IsSuccess, $"{ksql} failed: {response.Message}");
    }

}

// local environment helpers
public class EnvJoinIntegrationTests
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
