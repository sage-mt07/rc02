# 差分履歴: mapping_registry_introduction

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
Mapping namespace に動的型登録機構を追加

## 変更理由
設計書 key_value_flow.md 第8章の方針に合わせ、PropertyMeta から生成した KeyType/ValueType を一元管理するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `MappingRegistry` クラスを新設し、`KeyValueTypeMapping` を登録・取得する API を実装
- 設計ドキュメント `key_value_flow.md` および `mapping_namespace_doc.md` を更新
- `GetMapping(Type pocoType)` を公式APIとして明記

## 参考文書
- `docs/architecture/key_value_flow.md` セクション 8
