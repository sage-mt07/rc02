using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("ticks")]
public class Tick
{
    public string Symbol { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime At { get; set; }
}

public class BarsContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
    {
        b.Entity<Tick>().AsStream();
        // 代表例: WithWindow(...).Tumbling() + GroupBy で OHLC を作成（実装DSLに合わせて設定）
        // b.WithWindow<Tick>(new[]{1,5}, t=>t.At, t=> new { t.Symbol }, key=> key)
        //  .Select<Bar1m>(w => new Bar1m { ... });
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
            .BuildContext<BarsContext>();

        await ctx.Set<Tick>().AddAsync(new Tick { Symbol = "ABC", Price = 100m, At = DateTime.UtcNow });
        Console.WriteLine("Produced one Tick. Configure window + group to build bars.");
    }
}

