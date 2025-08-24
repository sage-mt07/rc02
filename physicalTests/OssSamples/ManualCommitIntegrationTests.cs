using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace Kafka.Ksql.Linq.Tests.Integration;

public class ManualCommitIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ManualCommit_PersistsOffset()
    {
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
                await ctx.Samples.AddAsync(new ManualCommitContext.Sample { Id = i }, cancellationToken: sendCts.Token);
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

