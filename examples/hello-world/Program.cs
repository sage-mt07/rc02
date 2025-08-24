using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("hello-world")]
public class HelloMessage
{
    public int Id { get; set; }

    [AvroTimestamp]
    public DateTime CreatedAt { get; set; }

    public string Text { get; set; } = string.Empty;
}

public class HelloKafkaContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HelloMessage>();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var context = KsqlContextBuilder.Create()
            .UseConfiguration(configuration)
            .UseSchemaRegistry(configuration["KsqlDsl:SchemaRegistry:Url"]!)
            .EnableLogging(LoggerFactory.Create(builder => builder.AddConsole()))
            .BuildContext<HelloKafkaContext>();

        var message = new HelloMessage
        {
            Id = Random.Shared.Next(),
            CreatedAt = DateTime.UtcNow,
            Text = "Hello World"
        };

        await context.Set<HelloMessage>().AddAsync(message);
        // wait until the stream is ready
        await context.WaitForEntityReadyAsync<HelloMessage>(TimeSpan.FromSeconds(5));

        await context.Set<HelloMessage>().ForEachAsync(m =>
        {
            Console.WriteLine($"Received: {m.Text}");
            return Task.CompletedTask;
        });
    }
}
