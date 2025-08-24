# 差分履歴: architecture docs

🗕 2025-08-24 (JST)
🧐 作業者: assistant

## 差分タイトル
MappingRegistry 対応によるアーキテクチャ文書更新

## 変更理由
Set<T>() と MappingRegistry 導入に伴い、旧 `EntitySet` と `MappingManager` の記述が残っていたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `EntitySet` → `Set<T>()` / `IEntitySet<T>` に統一
- `MappingManager` や `PocoMapper` の参照を `MappingRegistry` に置換
- `ctx.Messaging` の説明を削除し、`AddAsync` を中心に記述
- `key_value_flow.md` を簡素化し、サンプルコードを更新

## 参考文書
- docs/architecture/entityset_to_messaging_story.md
- docs/architecture/key_value_flow.md
- docs/architecture/query_ksql_mapping_flow.md
- docs/architecture/query_to_addasync_sample.md
