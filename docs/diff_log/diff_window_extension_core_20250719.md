# 差分履歴: window_extension_core

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
ModelBuilderWindowExtensions を OSS 本体へ移動

## 変更理由
Window DSL をサンプルだけでなくライブラリ本体で利用可能にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `examples/daily-comparison/DailyComparisonLib/ModelBuilderWindowExtensions.cs` を `src/Core/Modeling/` 配下に移動
- 名前空間を `Kafka.Ksql.Linq.Core.Modeling` に変更
- サンプル `KafkaKsqlContext` から新しい名前空間を参照

## 参考文書
- `docs/diff_log/diff_daily_comparison_extensionmove_20250719.md`
