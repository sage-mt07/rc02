# 差分履歴: TimeFrame dayKey

🗕 2025-08-24 JST
🧐 作業者: 鳴瀬

## dayKey parameter for TimeFrame
日足以降の足生成に必要な基準日列を指定できるよう、`TimeFrame` DSL に第2引数 `dayKey` を追加しました。

## 変更理由
MarketSchedule の日付列を明示し、長期足作成時の基準日を決定するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlQueryable.TimeFrame` に `dayKey` 引数を追加
- `MethodCallCollectorVisitor` と `TumblingAnalyzer` が dayKey を解析・検証
- `BasedOnSpec` と各アダプターに dayKey を格納
- 単体テストとドキュメントを dayKey 対応に更新

## 参考文書
- `docs/chart.md` の例示コード
