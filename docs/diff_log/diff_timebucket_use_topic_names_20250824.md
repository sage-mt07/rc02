# 差分履歴: timebucket_use_topic_names

🗕 2025年8月24日（JST）
🧐 作業者: assistant

## 差分タイトル
TimeBucket fetches by stored topic names

## 変更理由
- Avoid recomputing topic names inside context and ensure scans use resolved live/final topics

## 追加・修正内容（反映先: oss_design_combined.md）
- `ITimeBucketContext.Set` now takes a topic name and period
- `TimeBucket.ToListAsync` aggregates results from `_finalTopic` and `_liveTopic` via context sets

## 参考文書
- docs/chart.md
