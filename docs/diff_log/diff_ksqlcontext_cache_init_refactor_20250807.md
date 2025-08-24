# 差分履歴: ksqlcontext_cache_init_refactor

🗕 2025年8月7日（JST）
🧐 作業者: assistant

## 差分タイトル
Materialized生成とKafkaStream起動処理のリファクタリング

## 変更理由
キャッシュ初期化で反射を多用しKafkaStream起動も不安定だったため、テストコードと同様のシンプルな方式に統一する。

## 追加・修正内容（反映先: oss_design_combined.md）
- MaterializedをジェネリックCreateメソッドで生成し、反射使用を最小化
- KafkaStreamをStartAsync後にRUNNING状態まで待機する安定化処理を追加
- MultiStreamizKafkaStreamsを簡素化し、状態監視とストア取得リトライを実装

## 参考文書
- `physicalTests/Streamiz/StreamizRocksDbTests.cs`
