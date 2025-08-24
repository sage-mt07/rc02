using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("sql-orders")]
public class SqlOrder
{
    public int Id { get; set; }

    [AvroTimestamp]
    public DateTime CreatedAt { get; set; }

    public string Text { get; set; } = string.Empty;
}

public class SqlKafkaContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SqlOrder>();
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
            .BuildContext<SqlKafkaContext>();

        var message = new SqlOrder
        {
            Id = Random.Shared.Next(),
            CreatedAt = DateTime.UtcNow,
            Text = "Sync Sample"
        };

        await context.Set<SqlOrder>().AddAsync(message);
        // wait briefly for message to be published
        await Task.Delay(500);

        await context.Set<SqlOrder>().ForEachAsync(m =>
        {
            Console.WriteLine($"Kafka received: {m.Text}");
            return Task.CompletedTask;
        });
    }
}
