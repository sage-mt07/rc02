# topic_fluent_api_extension バリエーション設計

🗕 2025年6月27日（JST）
🧐 作業者: 詩音（テストエンジニアAI）

## 目的

Kafka トピック設定 DSL の学習段階に合わせたサンプルコードを提供し、Fluent API 拡張の利用イメージを共有する。

## 段階別プラン

### Beginner
- トピック名のみを指定する最小例

### Intermediate
- パーティション数とレプリケーション係数を Fluent API で指定する例

### Advanced
- 既存トピックとの衝突有無を確認しつつ `IsManaged` 設定を組み合わせる例
- 追加の拡張として `WithRetention` や `WithCleanupPolicy` を利用する例
- エラーハンドリング時の注意点もコメントで補足

