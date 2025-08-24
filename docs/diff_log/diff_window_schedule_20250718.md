# 差分履歴: window_schedule_design

🗕 2025年7月18日（JST）
🧐 作業者: codex

## 差分タイトル
MarketSchedule ベースの Window 拡張設計を追加

## 変更理由
トレーディング時間が日毎に変動する市場に対応するため、Window の生成基準を MarketSchedule POCO から取得できるようにする設計方針を明記した。

## 追加・修正内容（反映先: oss_design_combined.md）
- `docs/architecture_restart.md` に MarketSchedulePoco 仕様と Open/Close 範囲での足生成ルールを追記
- `docs/api_reference.md` に `.Window().BaseOn<TSchedule>(keySelector)` 行を追加
- `src/EventSetScheduleWindowExtensions.cs` に BaseOn 実装を追加し、スケジュールの Open/Close 範囲内のみデータを対象とするフィルタを構築
- Open/Close プロパティを `[ScheduleOpen]`/`[ScheduleClose]` 属性で示す方式を提案
- 足別トピックを TableCache 有効で扱う設計を追記
- `features/window/instruction.md` を新設し要件を共有
- Fluent API `IQueryable<T>.Window().BaseOn<TSchedule>(keySelector)` の定義を明記
- 日足生成時に `ScheduleClose` が 6:30 の場合、6:30 未満データを終値として扱うルールを追記

## 参考文書
- `docs/architecture_restart.md`
- `docs/api_reference.md`
