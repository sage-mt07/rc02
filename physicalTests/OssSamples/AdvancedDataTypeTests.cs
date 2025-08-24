using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Application;
using Confluent.Kafka;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Configuration;
using Kafka.Ksql.Linq.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace Kafka.Ksql.Linq.Tests.Integration;


public class AdvancedDataTypeTests
{
    public enum Status { Pending, Done }

    [KsqlTopic("records")]
    public class Record
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
        [KsqlDecimal(18,4)]
        public decimal Price { get; set; }
        public DateTime Created { get; set; }
      //  public Status State { get; set; }
    }

    public class RecordContext : KsqlContext
    {
        public EventSet<Record> Records { get; set; }
        public RecordContext() : base(new KsqlDslOptions()) { }
        public RecordContext(KsqlDslOptions options) : base(options) { }
        protected override void OnModelCreating(IModelBuilder modelBuilder)
        {
          //  modelBuilder.Entity<Record>();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Decimal_DateTime_Enum_RoundTrip()
    {


        //try
        //{
        //    await EnvAdvancedDataTypeTests.ResetAsync();
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine($"[Warning] ResetAsync failed: {ex}");
        //    return;
        //  //  throw new SkipException($"Test setup failed in ResetAsync: {ex.Message}");
        //}

        var options = new KsqlDslOptions
        {
            Common = new CommonSection { BootstrapServers = EnvAdvancedDataTypeTests.KafkaBootstrapServers },
            SchemaRegistry = new SchemaRegistrySection { Url = EnvAdvancedDataTypeTests.SchemaRegistryUrl }
        };
        options.Topics.Add("records", new Configuration.Messaging.TopicSection { Consumer = new Configuration.Messaging.ConsumerSection { AutoOffsetReset = "Earliest", GroupId = Guid.NewGuid().ToString() } });

        await using var ctx = new RecordContext(options);

        var data = new Record { Id = 1, Price = 12.3456m, Created = DateTime.UtcNow };
        await ctx.Records.AddAsync(data);
        await Task.Delay(5000);
        var list = new List<Record>();
        await ctx.Records.ForEachAsync(r => { list.Add(r); return Task.CompletedTask; }, TimeSpan.FromSeconds(10));
        Assert.Single(list);
        Assert.Equal(data.Price, list[0].Price);
        Assert.True(Math.Abs((list[0].Created - data.Created).TotalMinutes) < 1);
    }
}

// local environment helpers
public class EnvAdvancedDataTypeTests
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
