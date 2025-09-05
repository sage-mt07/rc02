using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("events-windowing")]
public class Evt { public string Key { get; set; } = ""; public DateTime At { get; set; } }

public class WindowContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
    {
        b.Entity<Evt>().AsStream();
        // 代表例: Hopping/Session の宣言（実装DSLに合わせて記述）
        // b.WithWindow<Evt>(hopping: TimeSpan.FromMinutes(5), advance: TimeSpan.FromMinutes(1), ...);
        // b.WithSessionWindow<Evt>(gap: TimeSpan.FromMinutes(2), ...);
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
            .BuildContext<WindowContext>();

        await ctx.Set<Evt>().AddAsync(new Evt { Key = "X", At = DateTime.UtcNow });
        Console.WriteLine("Produced one Evt. Configure hopping/session windows per docs.");
    }
}

