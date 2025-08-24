# EntitySet から Messaging までの利用ストーリー

🗕 2025年7月13日（JST）
🧐 作成者: 広夢・楠木

本ドキュメントでは、新アーキテクチャに基づく基本的な利用フローを示します。
`Set<T>()` で取得した `IEntitySet<T>` から `AddAsync` を呼び出して Kafka にメッセージを
送信するまでの流れをサンプルコードと共に記載します。設計意図とベストプラクティスを
理解することで、各レイヤーの役割分担を把握してください。

## 1. 事前準備

1. `QueryAnalyzer` で取得した `QuerySchema` を `KsqlContext` に登録する
2. `KsqlContext` を DI コンテナで管理する
3. `MappingRegistry` が POCO の key/value 変換を自動で行う

## 2. サンプルコード

```csharp
public class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}

class PaymentContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder builder)
    {
        builder.Entity<Payment>();
    }
}

var services = new ServiceCollection();
services.AddKsqlContext<PaymentContext>();

var provider = services.BuildServiceProvider();
var ctx = provider.GetRequiredService<PaymentContext>();

var payment = new Payment { Id = 1, Amount = 100m };
await ctx.Set<Payment>().AddAsync(payment);
```

## 3. ベストプラクティス

 - `QueryAnalyzer` から得たスキーマは再利用し、毎回解析し直さない
 - `KsqlContext` はスコープライフサイクルを推奨し、使い回しを避ける
 - 送信前に生成された KSQL 文をログで確認する
 - `AddAsync` は失敗時にリトライポリシーを設定する

## 4. 参考資料

- [key_value_flow.md](./key_value_flow.md) – 各レイヤーの責務概要
- [api_reference.md の Fluent API ガイドライン](../api_reference.md#fluent-api-guide)

## 5. 最新更新 (2025-08-24)
`Set<T>()` と `MappingRegistry` の統合に合わせて、`Messaging` 層を直接意識せず
`AddAsync` だけで送信できるように記述を刷新しました。

