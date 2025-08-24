using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("basic-produce-consume")]
public class BasicMessage
{
    public int Id { get; set; }

    [AvroTimestamp]
    public DateTime CreatedAt { get; set; }

    public string Text { get; set; } = string.Empty;
}

public class BasicContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BasicMessage>();
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
            .BuildContext<BasicContext>();

        var message = new BasicMessage
        {
            Id = Random.Shared.Next(),
            CreatedAt = DateTime.UtcNow,
            Text = "Basic Flow"
        };

        await context.Set<BasicMessage>().AddAsync(message);
        // wait briefly for message to be published
        await Task.Delay(500);

        await context.Set<BasicMessage>().ForEachAsync(m =>
        {
            Console.WriteLine($"Consumed message: {m.Text}");
            return Task.CompletedTask;
        });
    }
}
