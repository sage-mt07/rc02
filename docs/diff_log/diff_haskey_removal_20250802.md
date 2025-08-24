# 差分履歴: haskey_removal

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
HasKey API の削除

## 変更理由
Fluent API から HasKey を廃止し、キー指定を属性や他の手段に統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `IEntityBuilder<T>` と `EntityModelBuilder<T>` から HasKey メソッドを削除
- テストコードとドキュメントの HasKey 利用例を削除

## 参考文書
- docs/namespaces/core_namespace_doc.md
