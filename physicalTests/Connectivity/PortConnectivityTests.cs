using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq;
using Confluent.Kafka;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Application;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

//[TestCaseOrderer("Kafka.Ksql.Linq.Tests.Integration.PriorityOrderer", "Kafka.Ksql.Linq.Tests.Integration")]
public class PortConnectivityTests
{
[Fact]
//[TestPriority(1)]
    public void Kafka_Broker_Should_Be_Reachable()
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = EnvPortConnectivityTests.KafkaBootstrapServers }).Build();
        var meta = admin.GetMetadata(TimeSpan.FromSeconds(10));
        Assert.NotEmpty(meta.Brokers);
    }

[Fact]
//[TestPriority(2)]
    public async Task SchemaRegistry_Should_Be_Reachable()
    {
        using var http = new HttpClient();
        var resp = await http.GetAsync($"{EnvPortConnectivityTests.SchemaRegistryUrl}/subjects");
        Assert.True(resp.IsSuccessStatusCode);
    }

[Fact]
//[TestPriority(3)]
    public async Task KsqlDb_Should_Be_Reachable()
    {
        await using var ctx = EnvPortConnectivityTests.CreateContext();
        var result = await ctx.ExecuteStatementAsync("SHOW TOPICS;");
        Assert.True(result.IsSuccess);
    }
}

// local environment helpers
static class EnvPortConnectivityTests
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
