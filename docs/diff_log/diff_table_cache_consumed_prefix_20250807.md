# 差分履歴: table_cache_consumed_prefix

🗕 2025-08-07 (JST)
🧐 作業者: assistant

## 差分タイトル
Search Consumed type by prefix to support renamed Streamiz versions

## 変更理由
Explicit name lookup failed when Streamiz provided ConsumedInternal instead of Consumed/Consumed2.
Prefix-based discovery prevents InvalidOperationException across versions.

## 追加・修正内容（反映先: oss_design_combined.md）
- Scan Streamiz assembly for generic types starting with "Consumed" and two generic parameters.
- Remove hardcoded type names; throw only if no such type exists.

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "UseTableCache"
