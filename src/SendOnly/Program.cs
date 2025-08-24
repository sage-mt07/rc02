using Confluent.Kafka;
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
        var order = new Order
        {
            Id = Random.Shared.Next(),
            Amount = Random.Shared.Next(1, 100)
        };
        await context.Set<Order>().AddAsync(order);
        Console.WriteLine($"Sent Order: {order.Id}");
    }
    catch (KafkaException ex)
    {
        Console.WriteLine($"Kafka error: {ex.Error.Reason}");
    }
    await Task.Delay(1000);
}
