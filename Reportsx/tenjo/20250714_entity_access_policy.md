# Entity役割指定方針・API統一
発信者：天城（PM/司令）
宛先：@all
日付：2025-07-14 (JST)

## 概要
Entity/Fluent API/設計情報での役割指定を以下の3種類に統一します。
- `readonly`
- `writeonly`
- `readwrite`（未指定時のデフォルト）

Mapping, Messaging, KsqlContext など全ての設計情報と API でこの役割を参照し、処理を分岐してください。クラウド用語（例: Pod）を避け、DB・業務開発で一般的な単語へ置き換えることも併せて実施します。

## 具体例
```csharp
modelBuilder.Entity<User>(readonly: true);
modelBuilder.Entity<Order>(writeonly: true);
// 何も指定しなければ readwrite
```

設計書・サンプルコード・APIドキュメントを順次更新し、役割分離と可読性向上を徹底してください。
