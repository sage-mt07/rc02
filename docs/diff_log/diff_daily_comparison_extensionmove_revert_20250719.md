# 差分履歴: daily_comparison_extensionmove_revert

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
ModelBuilderWindowExtensions の配置変更を取り消し

## 変更理由
一旦サンプル側でのみ利用するため、本体ライブラリへの移動を撤回する。

## 追加・修正内容（反映先: oss_design_combined.md）
- `src/ModelBuilderWindowExtensions.cs` を削除し、`examples/daily-comparison/DailyComparisonLib/` に配置
- 既存プロジェクトからの参照は変わらず、名前空間 `Kafka.Ksql.Linq` で暫定利用

## 参考文書
- `docs/changes/20250719_progress.md` 該当エントリ
