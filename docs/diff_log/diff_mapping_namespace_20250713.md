# 差分履歴: mapping_namespace_redefinition

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
Mapping namespace を POCO ⇄ key/value 変換専用に再設計

## 変更理由
PM 指示により、Mapping 層の責務を明確化し Query から渡される `QuerySchema`
のみを利用する構造へ簡素化するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `PocoMapper` クラスを新設し `ToKeyValue` `FromKeyValue` API を提供
- 旧 `IMappingManager` と `MappingManager` を削除
- サンプル登録 `AddSampleModels` から MappingManager 依存を除去
- ドキュメント `mapping_namespace_doc.md` を追加し責任境界を明記

## テスト結果概要
- `dotnet test` 実行で全テスト成功を確認

## 参考文書
- `docs/structure/naruse/key_value_flow_naruse.md`
- `docs/architecture/query_to_addasync_sample.md`
