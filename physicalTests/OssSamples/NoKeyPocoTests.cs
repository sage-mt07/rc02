using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Application;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;

public class NoKeyPocoTests
{
    [KsqlTopic("records_no_key")]
    public class Record
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class RecordContext : KsqlContext
    {
        public EventSet<Record> Records { get; set; }    
        public RecordContext() : base(new KsqlDslOptions()) { }
        public RecordContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Record>();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendAndReceive_NoKeyRecord()
    {


        //await EnvNoKeyPocoTests.ResetAsync();

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvNoKeyPocoTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvNoKeyPocoTests.SchemaRegistryUrl },
            Topics =new Dictionary<string, Configuration.Messaging.TopicSection>()
        };
        options.Topics.Add("records_no_key",new Configuration.Messaging.TopicSection { Consumer=new Configuration.Messaging.ConsumerSection { AutoOffsetReset="Earliest", GroupId=Guid.NewGuid().ToString() } });
        await using var ctx = new RecordContext(options);

        var data = new Record { Id = 1, Name = "alice" };
        await ctx.Records.AddAsync(data);

        var list = new List<Record>();
        try
        {
            await ctx.Records.ForEachAsync(r => { list.Add(r); return Task.CompletedTask; }, TimeSpan.FromSeconds(10));

        }
        catch (OperationCanceledException) { }

        Assert.Single(list);
        Assert.Equal(data.Id, list[0].Id);
        Assert.Equal(data.Name, list[0].Name);

        await ctx.DisposeAsync();
    }
}

// local environment helpers
static class EnvNoKeyPocoTests
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
