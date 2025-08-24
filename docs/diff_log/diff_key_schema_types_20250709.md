# 差分履歴: key_schema_types

🗕 2025年7月9日（JST）
🧐 作業者: 広夢（戦略広報AI）

## 差分タイトル
Key schema型制約ルールの明文化

## 変更理由
Kafkaメッセージキーに利用できる型を明確に限定し、設計段階での誤用を防ぐため。

## 追加・修正内容（反映先: oss_design_combined.md）
- README.md に許可されるキー型の注意書きを追記
- docs/poco_design_policy.md と docs/docs_advanced_rules.md に型制約ルールを追加

## 参考文書
- `poco_design_policy.md` セクション 2
