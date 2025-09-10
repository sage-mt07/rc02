# 差分履歴: timebucket_merge_live_final

🗕 2025年8月24日（JST）
🧐 作業者: assistant

## 差分タイトル
TimeBucket live-only rule for non-1s periods

## 変更理由
- Simplify topic usage: periods over 1s produce only live topics; 1s period keeps final

## 追加・修正内容（反映先: oss_design_combined.md）
- TimeBucket constructor emits `<poco>_<period>_live` for periods >1s and `<poco>_1s_final` for 1s
- ToListAsync skips null topics and queries whichever exist
- Updated unit tests for live-only and 1s-final scenarios

## 参考文書
- docs/chart.md
