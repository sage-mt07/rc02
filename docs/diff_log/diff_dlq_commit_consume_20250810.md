# 差分履歴: dlq_commit_consume

🗕 2025-08-10 JST
🧐 作業者: 鳴瀬

## 差分タイトル
DLQ投入時のオフセットコミットとハンドラ最終失敗処理

## 変更理由
POCO変換失敗や処理最終失敗で同一レコードの再消費を防ぐため。

## 追加・修正内容（反映先: oss_design_combined.md）
- POCO化失敗時に DlqEnvelope を生成し Commit
- ForEachAsync の最終失敗で DLQ 投入後 Commit
- IDlqProducer / ICommitManager のDI導入

## 参考文書
- docs/changes/20250810_progress.md
