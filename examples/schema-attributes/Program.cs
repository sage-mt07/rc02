using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("schema-attributes-demo")]
public class Trade
{
    [KsqlKey(order: 0)] public string Symbol { get; set; } = string.Empty;
    [KsqlDecimal(precision: 18, scale: 4)] public decimal Price { get; set; }
    [AvroTimestamp] public DateTime Timestamp { get; set; }
}

public class SchemaAttrContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
        => b.Entity<Trade>().AsStream();
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
            .BuildContext<SchemaAttrContext>();

        await ctx.Set<Trade>().AddAsync(new Trade
        {
            Symbol = "FOO",
            Price = 123.4567m,
            Timestamp = DateTime.UtcNow
        });

        await Task.Delay(300);
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
        await ctx.Set<Trade>().ForEachAsync(t =>
        {
            Console.WriteLine($"Consumed: {t.Symbol} {t.Price} @ {t.Timestamp:O}");
            return Task.CompletedTask;
        }, cancellationToken: cts.Token);
    }
}
