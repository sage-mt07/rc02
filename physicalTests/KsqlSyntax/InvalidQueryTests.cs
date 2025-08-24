using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq;
using System;
using Kafka.Ksql.Linq.Application;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;


public class InvalidQueryTests
{
    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("SELECT ID, COUNT(*) FROM ORDERS GROUP BY ID;")]
    [InlineData("SELECT CASE WHEN ID=1 THEN 'A' ELSE 2 END FROM ORDERS EMIT CHANGES;")]
    public async Task GeneratedQuery_IsRejected(string ksql)
    {

        try
        {
            await EnvInvalidQueryTests.ResetAsync();

        }
        catch (System.Exception)
        {

        }
 
        await using var ctx = EnvInvalidQueryTests.CreateContext();
        var response = await ctx.ExecuteExplainAsync(ksql);
        Assert.False(response.IsSuccess, $"{ksql} unexpectedly succeeded");
    }
}

// local environment helpers
public class EnvInvalidQueryTests
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
