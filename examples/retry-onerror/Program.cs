using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("retry-demo")]
public class Item { public int Id { get; set; } public string Text { get; set; } = ""; }

public class RetryContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b) => b.Entity<Item>().AsStream();
}

class Program
{
    static async Task Main()
    {
        var cfg = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        var ctx = KsqlContextBuilder.Create()
            .UseConfiguration(cfg)
            .UseSchemaRegistry(cfg["KsqlDsl:SchemaRegistry:Url"]!)
            .EnableLogging(LoggerFactory.Create(b => b.AddConsole()))
            .BuildContext<RetryContext>();

        var set = ctx.Set<Item>().WithRetry(maxRetries: 3, retryInterval: TimeSpan.FromMilliseconds(200));
        set.StartErrorHandling().OnError(err => ErrorAction.Dlq);

        await set.AddAsync(new Item { Id = 1, Text = "Payload" });
        Console.WriteLine("Produced with retry + OnError(Dlq) configured.");
    }
}

