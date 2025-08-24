# 差分履歴: table_cache_consumed_fallback

🗕 2025-08-07 (JST)
🧐 作業者: assistant

## 差分タイトル
Resolve Consumed type via fallback for StreamBuilder.Table reflection

## 変更理由
Reflection for Consumed2 returned null causing NullReference during cache setup.

## 追加・修正内容（反映先: oss_design_combined.md）
- Try Consumed2 then fallback to Consumed type before invoking Consumed.With.
- Throw descriptive exception when neither Consumed type is available.

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "UseTableCache"
