# 差分履歴: timebucket_live_only

🗕 2025年9月10日（JST）
🧐 作業者: assistant

## 差分タイトル
TimeBucket live-only for periods >1s

## 変更理由
- Drop final topics for periods exceeding 1s, simplify writes and reads

## 追加・修正内容（反映先: oss_design_combined.md）
- Constructor builds `<poco>_<period>_live` for periods over 1s and `<poco>_1s_final` for 1s
- ToListAsync skips null topics and queries only existing ones
- TimeBucketWriter writes to the available topic without `toFinal`

## 参考文書
- docs/changes/20250824_progress.md
