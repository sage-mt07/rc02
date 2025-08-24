# diff_core_namespace_redesign_20250715.md

🗕 2025年7月15日（JST）
🧐 作業者: 広夢（戦略広報AI）

## 差分タイトル
Core namespace 再設計ドラフトの追加

## 変更理由
- POCO属性依存を完全に廃止する方針を反映するため
- LINQ式ベースのキー管理とトピック設定へ移行する準備として

## 追加・修正内容（反映先: oss_design_combined.md）
- `docs/core_namespace_redesign_plan.md` を新規作成
- `architecture_diff_20250711.md` の Core セクションを更新
- サンプル `Example1_Basic.cs` を `HasKey` 使用例に変更
- `EntityModelBuilder.HasKey` に xmldoc を追加

## 参考文書
- `architecture_restart_20250711.md` セクション Core
