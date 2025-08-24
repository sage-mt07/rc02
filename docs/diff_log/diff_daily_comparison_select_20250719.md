# 差分履歴: daily_comparison_select

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
RateCandle 生成DSLのサンプル追加

## 変更理由
Bar/Window定義を `KafkaKsqlContext.OnModelCreating` にまとめ、`Select<RateCandle>` で足生成まで宣言できるよう示すため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `WindowDslExtensions` に `WindowSelectionBuilder` と `WindowGrouping` を追加
- `KafkaKsqlContext.OnModelCreating` で `.Select<RateCandle>` 使用例を実装
- README とドキュメントに DSL チェーン例を追記

## 参考文書
- `docs/api_reference.md` バー定義セクション
