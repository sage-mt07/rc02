using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Microsoft.Extensions.Configuration;

[Topic("orders")]
public class Order
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}

public class OrderContext : KsqlContext
{
    public OrderContext(IConfiguration configuration) : base(configuration) { }
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>();
    }
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

await using var context = new OrderContext(configuration);

while (true)
{
    try
    {
        await context.Set<Order>().ForEachAsync((o, headers, meta) =>
        {
            Console.WriteLine($"Received Order: {o.Id}");
            return Task.CompletedTask;
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Receive error: {ex.Message}");
        await Task.Delay(1000);
    }
}
