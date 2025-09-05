using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

public class RefData { public string Key { get; set; } = ""; public string Value { get; set; } = ""; }

public class CacheContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
        => b.Entity<RefData>().AsTable(useCache: true);
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
            .BuildContext<CacheContext>();

        var rows = await ctx.Set<RefData>().ToListAsync();
        Console.WriteLine($"Rows: {rows.Count}");
    }
}

