using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("headers-meta-demo")]
public class Msg { public int Id { get; set; } public string Text { get; set; } = ""; }

public class HeadersContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b) => b.Entity<Msg>().AsStream();
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
            .BuildContext<HeadersContext>();

        var cid = Guid.NewGuid().ToString("N");
        await ctx.Set<Msg>().AddAsync(new Msg { Id = 1, Text = "hello" }, new() { ["cid"] = cid });

        await Task.Delay(200);
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
        await ctx.Set<Msg>().ForEachAsync((m, headers, meta) =>
        {
            Console.WriteLine($"Consumed: {m.Text} cid={headers.GetValueOrDefault(\"cid\")} partition={meta.Partition} offset={meta.Offset}");
            return Task.CompletedTask;
        }, cancellationToken: cts.Token);
    }
}
