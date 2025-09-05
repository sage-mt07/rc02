using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

[Topic("orders")]
public class Order { public int Id { get; set; } public int CustomerId { get; set; } public decimal Amount { get; set; } }

[Topic("customers")]
public class Customer { public int Id { get; set; } public string Name { get; set; } = string.Empty; public bool IsActive { get; set; } }

public class OrderSummary { public int OrderId { get; set; } public string CustomerName { get; set; } = string.Empty; }

public class ViewContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder b)
    {
        b.Entity<Order>();
        b.Entity<Customer>();
        b.Entity<OrderSummary>().ToQuery(q => q
            .From<Order>()
            .Join<Customer>((o, c) => o.CustomerId == c.Id)
            .Where((o, c) => c.IsActive)
            .Select((o, c) => new OrderSummary { OrderId = o.Id, CustomerName = c.Name }));
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
            .BuildContext<ViewContext>();

        // Produce sample rows
        await ctx.Set<Customer>().AddAsync(new Customer { Id = 1, Name = "Alice", IsActive = true });
        await ctx.Set<Order>().AddAsync(new Order { Id = 100, CustomerId = 1, Amount = 42.0m });

        await Task.Delay(500);
        using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(5));
        await ctx.Set<OrderSummary>().ForEachAsync(s => { Console.WriteLine($"{s.OrderId}:{s.CustomerName}"); return Task.CompletedTask; }, cancellationToken: cts.Token);
    }
}
