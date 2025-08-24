# Kafka.Ksql.Linq 開発者ガイド

## 🎯 新機能開発前の必読事項

**重複実装を避けるため、新機能開発前に必ずこのガイドを確認してください。**

---

## 📚 Namespace別詳細ドキュメント

### 📋 概要
各namespaceの詳細設計は、以下の専用ドキュメントで管理されています。  
**変更作業前には必ず該当namespaceのドキュメントを確認してください。**

### 🏗️ 詳細設計ドキュメント一覧

| Namespace | 責務 | ドキュメント | 変更頻度 |
|-----------|------|-------------|----------|
| **Application** | 開発者向けAPI、設定管理、スキーマ自動登録 | [Application 詳細設計](docs/architecture/namespaces/Application.md) | 🔴 高 |
| **Serialization** | Avroスキーマ生成、型安全シリアライゼーション | [Serialization 詳細設計](docs/architecture/namespaces/Serialization.md) | 🔴 高 |
| **Core** | ビジネスロジック抽象化、EntityModel構築 | [Core 詳細設計](docs/architecture/namespaces/Core.md) | 🔴 高 |
| **Window** | ウィンドウ確定処理、確定足生成・配信 | [Window 詳細設計](docs/architecture/namespaces/Window.md) | 🔴 高 |
| **Messaging** | Producer/Consumer管理、エラーハンドリング | [Messaging 詳細設計](docs/architecture/namespaces/Messaging.md) | 🔴 高 |
| **Query** | LINQ→KSQL変換、クエリ実行パイプライン | [Query 詳細設計](docs/architecture/namespaces/Query.md) | 🔴 高 |
| **StateStore** | ローカル状態管理、ウィンドウ処理、Ready状態監視 | [StateStore 詳細設計](docs/architecture/namespaces/StateStore.md) | 🟡 中 |

---

## 🔧 新機能追加時の判定フロー

### 1. 対象namespace特定
```
❓ どのnamespaceの機能ですか？
  → 上記表から該当namespaceを特定
  → 該当する詳細設計ドキュメントを確認
```

### 2. 既存機能確認
```
❓ 同様の機能は既に実装されていませんか？
  → 詳細設計ドキュメントの「主要クラス構成」セクションを確認
  → 「変更頻度・作業パターン」で類似パターンを確認
```

### 3. 実装方針決定
```
❓ 新規実装 vs 既存拡張？
  → 「💡 実装上の重要なポイント」セクションを参考
  → 「🔗 他Namespaceとの連携」で影響範囲を確認
```

---

## 📝 実装パターン

### エンティティ追加パターン
```csharp
// 1. Entity定義（POCO + 属性）
[Topic("orders")]
public class Order
{
    public string OrderId { get; set; }
    public decimal Amount { get; set; }
}

// 2. Context追加
public class MyKafkaContext : KafkaContext
{
    public IEntitySet<Order> Orders => Set<Order>();
}

// 3. 使用例
await context.Orders.AddAsync(new Order { ... });
var orders = await context.Orders.ToListAsync();
```

### エラーハンドリングパターン
```csharp
// 既存のErrorHandlingPolicyを使用
await context.Orders
    .OnError(ErrorAction.Retry)
    .WithRetry(3, TimeSpan.FromSeconds(1))
    .ToListAsync();
```

### Window操作パターン
```csharp
// 既存のWindow拡張を使用
var windowedOrders = context.Orders.Window(5); // 5分ウィンドウ
await windowedOrders.ToListAsync();
```

---

## 🚀 開発完了後の更新手順

### 1. 該当namespace詳細ドキュメントの更新
- **主要クラス構成**: 新規クラス・機能を追加
- **変更頻度・作業パターン**: 新しいパターンを記載
- **実装上の重要なポイント**: 実装例・ベストプラクティスを追加

### 2. 他namespaceへの影響確認
- **🔗 他Namespaceとの連携**: 依存関係の更新
- 影響があるnamespaceのドキュメントも更新

### 3. 全体概要の更新
- [アーキテクチャ概要](docs/architecture/overview.md): 必要に応じて全体図・責務を更新

---

## 🔍 重複チェックリスト

新機能開発前に以下を確認：

- [ ] 該当namespaceの詳細設計ドキュメントを確認した
- [ ] 同じ責務のクラス・機能は既に存在しないか確認した
- [ ] 似たような実装パターンが既にないか確認した
- [ ] 他namespaceとの連携影響を確認した
- [ ] 設計制約・注意事項を確認した

---

## 📚 参考ドキュメント

### 🏗️ アーキテクチャ
- [全体概要](docs/architecture/overview.md) - システム全体のアーキテクチャ
- [作業指示書](docs/architecture/namespace_doc_work_instruction.md) - ドキュメント作成指針

### 🎯 設計方針
- **POCO属性主導**: `[Topic]` 属性を中心とし、キーはプロパティ定義順から自動生成（`Key` 属性は使用しない）
- **EF風API**: DbContextライクな親しみやすいインターフェース
- **型安全性**: ジェネリクス活用によるコンパイル時型チェック
- **Fail-Fast**: 初期化時エラーの即座終了
- **Stream専用関数**: `MIN`/`MAX` を含むクエリは自動的に `CREATE STREAM` として扱います。`CREATE TABLE` を指定するとエラーとなります。

### 🔧 コーディング規約
- **nullable reference types**: 有効化必須
- **ConfigureAwait(false)**: 非同期メソッドで使用
- **IDisposable**: 適切なリソース解放実装

## 🧪 Integration テスト運用

- 物理テスト実行前に `TestEnvironment.ResetAsync()` を実行し、既存スキーマやトピックをすべて削除した上で `CREATE TABLE/STREAM` を自動作成します。
- セットアップ直後に `curl http://localhost:8081/subjects` を実行し、各サブジェクトが登録されていることを確認してください。
- スキーマ登録時に `409 NAME_MISMATCH` が発生した場合は対象サブジェクトを削除してから `ResetAsync()` を再実行します。
- `ResetAsync` は何度呼び出しても安全で、同一スキーマの再登録によるエラーは発生しません。

---

## ❓ 疑問・質問

### namespace選択に迷ったら
1. **ユーザー向けAPI**: Application
2. **POCO属性・モデル**: Core  
3. **Kafka通信**: Messaging
4. **スキーマ・シリアライゼーション**: Serialization
5. **LINQ→KSQL変換**: Query
6. **ローカル状態管理**: StateStore
7. **ウィンドウ確定処理**: Window

### 実装方針に迷ったら
該当namespaceの詳細設計ドキュメントの「💡 実装上の重要なポイント」セクションを参照してください。

---

*各namespace詳細ドキュメントと合わせてご活用ください。*