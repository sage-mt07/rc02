# 差分履歴: table_cache_string_key

🗕 2025-08-22 JST
🧐 作業者: 鏡花（品質監査AI）

## 差分タイトル
文字列キー化された TableCache と Streamiz フィルタ

## 変更理由
RocksDB キャッシュで NUL 区切りの前方一致フィルタを可能にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- ITableCache.ToListAsync にフィルタ引数を追加
- TableCache で文字列キー前方一致フィルタを実装
- KsqlContextCacheExtensions が Avro キーを NUL 区切り文字列キーへ変換
- Mapping に FormatKeyForPrefix と CombineFromStringKeyAndAvroValue を追加

## 参考文書
- docs_advanced_rules.md
