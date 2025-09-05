using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("sales")]
public class Sale { public string StoreId { get; set; } = ""; public decimal Amount { get; set; } public DateTime At { get; set; } }
public class Sales1m { public string StoreId { get; set; } = ""; public DateTime WindowStart { get; set; } public decimal Total { get; set; } }

public class TumblingContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
    {
        b.Entity<Sale>().AsStream();
        // 代表的な例。実装の Window DSL に合わせて利用
        // b.WithWindow<Sale>(new[]{1}, s => s.At, s => new { s.StoreId }, k => k)
        //  .Select<Sales1m>(w => new Sales1m { StoreId = w.Key.StoreId, WindowStart = w.BarStart, Total = w.Source.Sum(x => x.Amount) });
    }
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
            .BuildContext<TumblingContext>();

        await ctx.Set<Sale>().AddAsync(new Sale{ StoreId="S1", Amount=100m, At=DateTime.UtcNow});
        Console.WriteLine("Produced one Sale. Configure window DSL per docs to aggregate.");
    }
}

