# 差分履歴: schema_builder_integration

🗕 2025年7月19日（JST）
🧐 作業者: assistant

## 差分タイトル
SchemaBuilder による動的 Avro スキーマ生成機能を追加

## 変更理由
C# クラス変更に追随できるよう、メモリ上でスキーマを生成し直接利用するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- DynamicSchemaGenerator を Messaging 層に追加
- Producer/Consumer で型からスキーマを生成するよう更新
- README に自動スキーマ生成の説明を追記

## 参考文書
- `docs_advanced_rules.md` セクション 2
