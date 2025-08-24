# 差分履歴: addasync_standardization

🗕 2025年7月27日（JST）
🧐 作業者: naruse

## 差分タイトル
AddAsync API への統一と自動フローサンプル追加

## 変更理由
- ProduceAsync 呼称が残っていた箇所を整理し、公式 API を AddAsync に統一
- LINQ クエリから QueryBuilder・MappingManager 経由で AddAsync へ至る流れを
  ドキュメントとテストで示すため

## 追加・修正内容（反映先: oss_design_combined.md）
- `docs/architecture/key_value_flow.md` など3箇所でサンプルを AddAsync に更新
- 新ドキュメント `query_to_addasync_sample.md` を追加
- `AutomaticQueryFlowTests` テストを新設

## 参考文書
- `getting-started.md` セクション 5 プロデュース操作
- `docs_advanced_rules.md` セクション 2
