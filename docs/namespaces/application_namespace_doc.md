# Kafka.Ksql.Linq.Application namespace 責務一覧

## 📋 概要
**KSQLコンテキストの構築・設定・初期化を担う上位層namespace**

Core層の抽象化（`KafkaContextCore`）を継承し、Schema Registry連携・Producer/Consumer管理・Cache統合など本格的なKafka機能を提供する実装層です。

---

## 🏗️ 主要クラス群

### **KsqlContextBuilder**
**責務**: KSQLコンテキストの段階的構築（Builderパターン）

```csharp
// 使用例
var context = KsqlContextBuilder.Create()
    .UseSchemaRegistry("http://localhost:8081")
    .EnableLogging(loggerFactory)
    .ConfigureValidation(autoRegister: true, failOnErrors: true)
    .BuildContext<MyKsqlContext>();
```

- **設計意図**: Fluent APIによる型安全な設定構築
- **主要機能**:
  - Schema Registry設定（URL/Config/Client指定）
  - ロギング設定
  - 検証設定（自動登録、エラー処理、プリウォーミング）
  - タイムアウト設定
  - ジェネリック型でのコンテキスト生成

### **KsqlContextOptions + Extensions**
**責務**: コンテキスト設定値の集約管理と検証

- **核心機能**:
  - Schema Registry Client必須チェック
  - タイムアウト値検証
  - 自動スキーマ登録制御
  - キャッシュプリウォーミング制御
  - エラーハンドリング制御

- **拡張メソッド群**:
  - `UseSchemaRegistry()` - URL/Config指定でのクライアント生成
  - `EnableLogging()` - LoggerFactory設定
  - `ConfigureValidation()` - 検証オプション一括設定
  - `WithTimeouts()` - タイムアウト設定

### **AvroSchemaInfoExtensions**
**責務**: Avroスキーマ情報の操作・変換ユーティリティ

```csharp
// Subject名生成
var keySubject = schemaInfo.GetKeySubject();     // "{TopicName}-key"
var valueSubject = schemaInfo.GetValueSubject(); // "{TopicName}-value"

// Stream/Table判定
var type = schemaInfo.GetStreamTableType();      // "Table" or "Stream"

// キー型判定  
var keyType = schemaInfo.GetKeyTypeName();       // "string", プロパティ型名, or "CompositeKey"
```

- **設計意図**: スキーマ関連処理の共通化、命名規則の統一
- **判定ロジック**: `HasCustomKey`プロパティベースでのStream/Table自動判別

---

## 🔗 継承・依存関係

### **継承構造**
```
KafkaContextCore (Core層)
    ↓ 継承
KsqlContext (Application層)
    ↓ 廃止予定
KafkaContext (互換性シム)
```

### **設定オプションの使い分け**
- **`KsqlContextOptions`** (Application層): Schema Registry、ログ、検証など上位機能の設定
- **`KafkaContextOptions`** (Core層): 検証モードなど基本設定のみ

### **外部依存関係**
- **Schema Registry**: `Confluent.SchemaRegistry.*`
- **設定管理**: `Microsoft.Extensions.Configuration`
- **ログ出力**: `Microsoft.Extensions.Logging`
- **Core抽象化**: `Kafka.Ksql.Linq.Core.*`

---

## ⚡ 実装の特徴

### **スキーマ自動登録フロー**
1. `OnModelCreating()` でモデル構築
2. `EntityModel` → `AvroEntityConfiguration` 変換
3. Schema Registry への同期登録実行
4. Kafka接続確認・DLQトピック生成

### **初期化戦略**
- **通常モード**: スキーマ登録 + Kafka接続確認を実行
- **テストモード**: `SkipSchemaRegistration = true` でスキーマ処理をスキップ
- **失敗時**: FATAL例外で即座にアプリケーション停止

### **Cache統合**
- TableCache設定に基づく自動バインディング作成
- エンティティ単位でのキャッシュ管理
- `RUNNING` 状態監視と警告ログ出力

---

## 🎯 責務境界

### **このnamespaceが担う責務**
- ✅ KSQLコンテキストの構築・設定管理
- ✅ Schema Registry連携の初期化
- ✅ 上位層サービス（Producer/Consumer/Cache）の統合
- ✅ Avroスキーマ情報の操作ユーティリティ

### **このnamespaceが担わない責務**  
- ❌ 実際のKafkaメッセージング処理（`Messaging`層）
- ❌ スキーマ登録の実装詳細（`Serialization`層）
- ❌ エンティティセットの具体的実装（ルート層 `EventSet<T>`）
- ❌ 低レベルKafka操作（`Infrastructure`層）

---

## 💡 利用パターン

### **基本的な初期化パターン**
```csharp
[KsqlTopic("orders")]
public class OrderEvent
{
    [KsqlKey(Order = 0)]
    public int Id { get; set; }
}

public class MyKsqlContext : KsqlContext
{
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderEvent>();
    }
}

// 使用
var context = KsqlContextBuilder.Create()
    .UseSchemaRegistry("http://localhost:8081")
    .EnableLogging(loggerFactory)
    .BuildContext<MyKsqlContext>();
```

### **設定重点パターン**  
```csharp
var options = KsqlContextBuilder.Create()
    .UseConfiguration(configuration)
    .ConfigureValidation(
        autoRegister: true,
        failOnErrors: false,      // 本番では緩い設定
        enablePreWarming: true)
    .WithTimeouts(TimeSpan.FromMinutes(2))
    .Build();
```

**このドキュメントにより、Application namespaceの責務と使用方法が明確になり、大規模ソース参照時の迷いを解消できます。**
