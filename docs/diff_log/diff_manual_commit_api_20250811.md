# 差分履歴: manual_commit_api

🗕 2025-08-11
🧐 作業者: naruse

## 差分タイトル
EventSet<T>.Commit API とコミット追跡

## 変更理由
手動コミットAPIを簡素化し、entityとオフセットの紐付けを可能にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- EventSet<T> に Commit(T entity) を追加。
- 内部 ICommitRegistrar と TrackCommitIfSupported を導入。
- 物理テストとドキュメントを Commit 呼び出し方式へ更新。

## 参考文書
- docs/api_reference.md
- docs/getting-started.md
