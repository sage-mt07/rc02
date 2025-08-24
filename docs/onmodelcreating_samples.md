# OnModelCreating サンプル集

以下は `modelBuilder.Entity<T>().ToQuery(...)` を用いた KSQL 定義例です。
Kafka クラスターやトピック設定は省略しています。

## 単純射影と絞り込み
```csharp
modelBuilder.Entity<ActiveOrder>().ToQuery(q => q
    .From<Order>()
    .Where(o => o.IsActive)
    .Select(o => new ActiveOrder { Id = o.Id, Amount = o.Amount }));
```

## 2テーブル内部結合
```csharp
modelBuilder.Entity<OrderPayment>().ToQuery(q => q
    .From<Order>()
    .Join<Payment>((o, p) => o.Id == p.OrderId)
    .Select((o, p) => new OrderPayment { OrderId = o.Id, Paid = p.Paid }));
```

## 集計（GROUP BY → Push 自動化）
```csharp
modelBuilder.Entity<OrderStats>().ToQuery(q => q
    .From<Order>()
    .GroupBy(o => o.CustomerId)
    .Select(g => new OrderStats { CustomerId = g.Key, Total = g.Sum(x => x.Amount) })
    .AsPush());
```

## HAVING 相当（集計後の条件）
```csharp
modelBuilder.Entity<BigCustomer>().ToQuery(q => q
    .From<Order>()
    .GroupBy(o => o.CustomerId)
    .Having(g => g.Sum(x => x.Amount) > 1000)
    .Select(g => new BigCustomer { CustomerId = g.Key, Total = g.Sum(x => x.Amount) }));
```

## CASE（型統一ルール厳守）
```csharp
modelBuilder.Entity<CustomerStatus>().ToQuery(q => q
    .From<Order>()
    .GroupBy(o => o.CustomerId)
    .Select(g => new CustomerStatus
    {
        CustomerId = g.Key,
        Status = g.Sum(x => x.Amount) > 1000 ? "VIP" : "Regular"
    }));
```

## DECIMAL 精度・スケール検証付き集計
```csharp
public class Order
{
    public int CustomerId { get; set; }
    [KsqlDecimal(18,2)]
    public decimal Amount { get; set; }
}

modelBuilder.Entity<OrderTotal>().ToQuery(q => q
    .From<Order>()
    .GroupBy(o => o.CustomerId)
    .Select(g => new OrderTotal { CustomerId = g.Key, Total = g.Sum(x => x.Amount) }));
```
