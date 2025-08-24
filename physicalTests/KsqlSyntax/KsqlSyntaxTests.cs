using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq;
using Confluent.Kafka;
using System;
using Kafka.Ksql.Linq.Application;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;


public class KsqlSyntaxTests
{
    public KsqlSyntaxTests()
    {
        EnvKsqlSyntaxTests.ResetAsync().GetAwaiter().GetResult();

        using var ctx = EnvKsqlSyntaxTests.CreateContext();
        var r1 = ctx.ExecuteStatementAsync(
            "CREATE STREAM IF NOT EXISTS source (id INT) WITH (KAFKA_TOPIC='source', VALUE_FORMAT='AVRO', PARTITIONS=1);"
        ).Result;
        Console.WriteLine($"CREATE STREAM result: {r1.IsSuccess}, msg: {r1.Message}");

        foreach (var ddl in TestSchema.GenerateTableDdls())
        {
            var r = ctx.ExecuteStatementAsync(ddl).Result;
            Console.WriteLine($"DDL result: {r.IsSuccess}, msg: {r.Message}");
        }
    }

    // 生成されたクエリがksqlDBで解釈可能か確認
    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("CREATE STREAM test_stream AS SELECT * FROM source EMIT CHANGES;")]
    [InlineData("SELECT CustomerId, COUNT(*) FROM orders GROUP BY CustomerId EMIT CHANGES;")]
    public async Task GeneratedQuery_IsValidInKsqlDb(string ksql)
    {


        var response = await ExecuteExplainDirectAsync(ksql);
        Assert.True(response.IsSuccess, $"{ksql} failed: {response.Message}");
    }

    private static async Task<KsqlDbResponse> ExecuteExplainDirectAsync(string ksql)
    {
        using var client = new HttpClient { BaseAddress = new Uri(EnvKsqlSyntaxTests.KsqlDbUrl) };
        var payload = new { ksql = $"EXPLAIN {ksql}", streamsProperties = new { } };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/ksql", content);
        var body = await response.Content.ReadAsStringAsync();
        var success = response.IsSuccessStatusCode && !body.Contains("\"error_code\"");
        return new KsqlDbResponse(success, body);
    }

}

// local environment helpers
public class EnvKsqlSyntaxTests
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
