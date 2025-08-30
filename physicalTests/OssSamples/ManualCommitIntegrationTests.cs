using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace Kafka.Ksql.Linq.Tests.Integration;

[Collection("DataRoundTrip")]
public class ManualCommitIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManualCommit_PersistsOffset()
    {
        // Ensure clean slate and environment readiness
        try { await PhysicalTestEnv.Cleanup.DeleteSubjectsAsync(EnvManualCommitIntegrationTests.SchemaRegistryUrl, new[] { "manual_commit" }); } catch { }
        try { await PhysicalTestEnv.Cleanup.DeleteTopicsAsync(EnvManualCommitIntegrationTests.KafkaBootstrapServers, new[] { "manual_commit" }); } catch { }
        // Ensure environment is ready and topic exists
        await PhysicalTestEnv.Health.WaitForKafkaAsync(EnvManualCommitIntegrationTests.KafkaBootstrapServers, TimeSpan.FromSeconds(120));
        await PhysicalTestEnv.Health.WaitForHttpOkAsync($"{EnvManualCommitIntegrationTests.SchemaRegistryUrl}/subjects", TimeSpan.FromSeconds(120));
        var groupId = Guid.NewGuid().ToString();

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvManualCommitIntegrationTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvManualCommitIntegrationTests.SchemaRegistryUrl }
        };

        options.Topics.Add("manual_commit", new Kafka.Ksql.Linq.Configuration.Messaging.TopicSection
        {
            Consumer = new Kafka.Ksql.Linq.Configuration.Messaging.ConsumerSection
            {
                AutoOffsetReset = "Earliest",
                GroupId = groupId
            }
        });

        // produce five messages and commit at the third
        await using (var ctx = new ManualCommitContext(options))
        {
            using var sendCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            for (var i = 1; i <= 5; i++)
            {
                var attempts = 0;
                while (true)
                {
                    try
                    {
                        await ctx.Samples.AddAsync(new ManualCommitContext.Sample { Id = i }, cancellationToken: sendCts.Token);
                        break;
                    }
                    catch (Confluent.SchemaRegistry.SchemaRegistryException)
                    {
                        if (++attempts >= 3) throw;
                        await Task.Delay(500);
                    }
                }
            }

            using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await ctx.Samples.ForEachAsync((sample, _, _) =>
            {
                if (sample.Id == 3)
                {
                    ctx.Samples.Commit(sample); // manual: 実コミット / autocommit: no-op
                    consumeCts.Cancel();
                }
                return Task.CompletedTask;
            }, autoCommit: false, cancellationToken: consumeCts.Token);
        }

        // verify resuming from the committed offset
        await using (var ctx = new ManualCommitContext(options))
        {
            using var consumeCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            ManualCommitContext.Sample? received = null;
            await ctx.Samples.ForEachAsync((sample, _, _) =>
            {
                received = sample;
                ctx.Samples.Commit(sample);
                consumeCts.Cancel();
                return Task.CompletedTask;
            }, autoCommit: false, cancellationToken: consumeCts.Token);

            Assert.Equal(4, received!.Id);
        }
    }
}

public static class EnvManualCommitIntegrationTests
{
    internal const string SchemaRegistryUrl = "http://localhost:8081";
    internal const string KafkaBootstrapServers = "localhost:9092";
}
