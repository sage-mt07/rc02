# 差分履歴: schema_registration_cleanup

🗕 2025年8月2日（JST）
🧐 作業者: assistant

## 差分タイトル
MappingRegistryからAvroスキーマ文字列を取得するよう修正

## 変更理由
KsqlContextでAvro.Specificに直接依存せずにスキーマ登録を行うため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KeyValueTypeMappingにキー/値スキーマJSONを保持するプロパティを追加
- スキーマ登録処理でISpecificRecordを生成せず、保持済みスキーマ文字列を使用

## 参考文書
- `docs_advanced_rules.md` セクション 2
