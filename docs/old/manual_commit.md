# Manual Commitの利用例

Kafka.Ksql.Linq では、自動コミットを無効化し、`Commit(entity)` を呼び出して手動コミットできるモードを提供しています。ここでは、`WithManualCommit()` を使用した場合の `ForEachAsync()` の振る舞いを説明します。

```csharp
var orders = context.HighValueOrders.WithManualCommit();
await foreach (var order in orders.ForEachAsync())
{
    Process(order);
    orders.Commit(order);
}
```

`WithManualCommit()` を指定しない場合、`ForEachAsync()` はエンティティ `T` をそのまま返します。自動コミットが行われるため、手動で `Commit(entity)` などを呼び出す必要はありません。

```csharp
await foreach (var order in context.Orders.ForEachAsync())
{
    Console.WriteLine(order.OrderId);
}
```
