# 差分履歴: query_schema_design

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
QuerySchema に Key/Value メタ情報を追加

## 変更理由
Query namespace 解析結果に論理 Key/Value 情報を提供し、MappingManager や Schema Registry 連携を容易にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KeyValueSchemaInfo` クラス新設
- `QuerySchema` に `KeyInfo` と `ValueInfo` を追加しクラス名・namespace・バージョンを保持
- EntityModel 保存文字列に Key/Value クラス名を含める
- ドキュメント `query_namespace_doc.md` に設計転換を追記

## 参考文書
- `docs/namespaces/query_namespace_doc.md`
