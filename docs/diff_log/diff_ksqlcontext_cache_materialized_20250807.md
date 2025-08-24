# 差分履歴: ksqlcontext_cache_materialized

🗕 2025年8月7日（JST）
🧐 作業者: assistant

## 差分タイトル
KsqlContextCacheExtensionsがAvroキー/値のMaterializedを使用

## 変更理由
AvroKey_To_RocksDb の動作を反映し、RocksDb.Asの代わりにMaterialized.Createでキャッシュを初期化するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `UseTableCache` が `Materialized.Create` を用いてAvroキー/値SerDesを設定
- 既存の `RocksDb.As` と `WithKeySerdes` / `WithValueSerdes` 反射呼び出しを削除

## 参考文書
- `physicalTests/Streamiz/StreamizRocksDbTests.cs`
