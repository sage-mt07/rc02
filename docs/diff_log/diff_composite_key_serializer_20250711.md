# 差分履歴: composite_key_serializer

🗕 2025年7月11日（JST）
🧐 作業者: 迅人（テスト自動化AI）

## 差分タイトル
RegisterSchemaAsync が string/Schema 両方に対応

## 変更理由
Confluent の AvroSerializer が schema string を渡すため、FakeSchemaRegistryClient が Schema 型のみ受け付けるとテストが失敗した。呼び出し元に応じて string または Schema を受け付けるよう修正。

## 追加・修正内容（反映先: oss_design_combined.md）
- RegisterSchemaAsync の引数を型判定し string/Schema 両対応に更新

## 参考文書
- `docs_advanced_rules.md` セクション 7
