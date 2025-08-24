# 差分履歴: topic_attribute_migration

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
ToTopic API の削除と KsqlTopic 属性への移行

## 変更理由
トピック名の指定を Fluent API から属性ベースに統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- ドキュメントの `.ToTopic()` 例を `[KsqlTopic]` 属性へ変更
- トピック設定テストを属性ベースにリファクタリング

## 参考文書
- docs/namespaces/application_namespace_doc.md
