# 差分履歴: eventset_modelbuilder_link

🗕 2025-08-02（JST）
🧐 作業者: 鳴瀬

## 差分タイトル
EventSetプロパティをModelBuilderへ登録しDDL処理へ接続

## 変更理由
public `EventSet<T>` プロパティがModelBuilderへ自動登録されず、後続の `CREATE STREAM/TABLE` 処理に反映されない問題を防ぐため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `InitializeEventSetProperties` が `ModelBuilder.AddEntityModel` を呼び出し、Fluent API設定を適用可能に
- 自動登録を検証するテスト `ConfigureModelEventSetRegistrationTests` を追加

## 参考文書
- `docs/change_summary_codex_20250801.md`
