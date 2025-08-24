# 差分履歴: RocksDbTableCache GetAll reflection

🗕 2025-08-06 (JST)
🧐 作業者: assistant

## 差分タイトル
Reflective All enumeration to avoid RuntimeBinderException

## 変更理由
Dynamic store instances implemented `IReadOnlyKeyValueStore` explicitly, causing RuntimeBinderException when calling `All()` via dynamic dispatch.

## 追加・修正内容（反映先: oss_design_combined.md）
- Invoke `IReadOnlyKeyValueStore.All` through reflection and iterate the returned enumerator.
- Ensure enumerator disposal and continue returning POCO values.

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "主要コンポーネント責務"
