# 差分履歴: timebucket_merge_live_final

🗕 2025年8月24日（JST）
🧐 作業者: assistant

## 差分タイトル
TimeBucket merges live and final topics

## 変更理由
- ToListAsync should resolve both live and final tables from POCO and period and return combined results

## 追加・修正内容（反映先: oss_design_combined.md）
- TimeBucket now scans `_period`-specific live and final RocksDB caches and concatenates rows
- Updated unit tests to seed live and final topics and verify merged retrieval

## 参考文書
- docs/chart.md
