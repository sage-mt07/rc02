# 差分履歴: mapping_schema_naming_rule

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
Mapping Registry のスキーマ命名規約を実装

## 変更理由
KeyType/ValueType の型名が ksqlDB 登録スキーマと一致していなかったため、完全修飾名を小文字化した `-key`/`-value` 形式へ統一する。

## 追加・修正内容（反映先: oss_design_combined.md）
- `MappingRegistry.Register` でスキーマ命名規則を自動適用
- テスト `MappingRegistryTests` を更新し命名ルールを検証
- 設計ドキュメント `key_value_flow.md`、`mapping_namespace_doc.md` を追記

## 参考文書
- `docs/architecture/key_value_flow.md` セクション 8
