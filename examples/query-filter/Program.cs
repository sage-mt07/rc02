using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("filter-demo")]
public class Event { public int Id { get; set; } public string Category { get; set; } = ""; }

public class FilterContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b) => b.Entity<Event>();
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
            .BuildContext<FilterContext>();

        await ctx.Set<Event>().AddAsync(new Event { Id = 1, Category = "A" });
        await ctx.Set<Event>().AddAsync(new Event { Id = 2, Category = "B" });

        await Task.Delay(300);
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
        await ctx.Set<Event>()
            .Where(e => e.Category == "A")
            .ForEachAsync(e => { Console.WriteLine($"A:{e.Id}"); return Task.CompletedTask; }, cancellationToken: cts.Token);
    }
}
