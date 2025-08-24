using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("orders")]
public class Order
{
    public int Id { get; set; }

    [DecimalPrecision(18, 2)]
    public decimal Amount { get; set; }
}

public class OrderContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>();
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
            .BuildContext<OrderContext>();

        var order = new Order
        {
            Id = Random.Shared.Next(),
            Amount = -42.5m
        };

        await context.Set<Order>().AddAsync(order);
        await Task.Delay(500);

        await context.Set<Order>()
            .OnError(ErrorAction.DLQ)
            .WithRetry(3)
            .ForEachAsync(o =>
            {
                if (o.Amount < 0)
                {
                    throw new InvalidOperationException("Amount cannot be negative");
                }
                Console.WriteLine($"Processed order {o.Id}: {o.Amount}");
                return Task.CompletedTask;
            });
    }
}
