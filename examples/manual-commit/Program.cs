using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("manual-commit-orders")]
public class ManualCommitOrder
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class ManualCommitContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ManualCommitOrder>();
    }

    public EventSet<ManualCommitOrder> Orders => Set<ManualCommitOrder>();
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
            .BuildContext<ManualCommitContext>();

        var order = new ManualCommitOrder
        {
            OrderId = Random.Shared.Next(),
            Amount = 10m
        };

        await context.Orders.AddAsync(order);
        await Task.Delay(500);

        await context.Orders.ForEachAsync(async (order, headers, meta) =>
        {
            Console.WriteLine($"Processing order {order.OrderId}: {order.Amount}");
            context.Orders.Commit(order);
        }, autoCommit: false);
    }
}
