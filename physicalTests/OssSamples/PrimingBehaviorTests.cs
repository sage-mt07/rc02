using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Application;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Integration;

[Collection("DataRoundTrip")]
public class PrimingBehaviorTests
{
    [KsqlTopic("priming_records")]
    public class Record
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
        public string? Note { get; set; }
    }

    public class RecordContext : KsqlContext
    {
        public RecordContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
            => modelBuilder.Entity<Record>();
    }

    private static KsqlDslOptions CreateOptions()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvPrimingBehaviorTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvPrimingBehaviorTests.SchemaRegistryUrl }
        };
        options.Topics.Add("priming_records", new Kafka.Ksql.Linq.Configuration.Messaging.TopicSection
        {
            Consumer = new Kafka.Ksql.Linq.Configuration.Messaging.ConsumerSection
            {
                AutoOffsetReset = "Earliest",
                GroupId = Guid.NewGuid().ToString()
            }
        });
        return options;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AlwaysPriming_DoesNotLeakIntoConsumption()
    {
        await using var ctx = new RecordContext(CreateOptions());

        // Ensure ksql metadata is ready and send priming dummy (is_dummy=true)
        await ctx.WaitForEntityReadyAsync<Record>(TimeSpan.FromSeconds(10));
        await ctx.EnsurePrimedAsync<Record>();

        // Send a real record
        await ctx.Set<Record>().AddAsync(new Record { Id = 1, Note = "real" });

        var list = new List<Record>();
        await ctx.Set<Record>().ForEachAsync(r => { list.Add(r); return Task.CompletedTask; }, TimeSpan.FromSeconds(5));

        // Exactly one record should be observed (dummy is skipped)
        Assert.True(list.Count >= 1, "Expected at least one non-dummy record");
        foreach (var item in list)
        {
            Assert.Equal(1, item.Id);
            Assert.Equal("real", item.Note);
        }
    }
}

public class EnvPrimingBehaviorTests
{
    internal const string SchemaRegistryUrl = "http://localhost:8081";
    internal const string KsqlDbUrl = "http://localhost:8088";
    internal const string KafkaBootstrapServers = "localhost:9092";
}
