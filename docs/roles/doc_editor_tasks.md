# ドキュメント編集タスク（葉月向け）

目的: README の導線に基づき、各ドキュメントの役割を明確化し、不要文書を削除・再編する。

## 実施内容（第1弾: 入口整備とリンク整合）
- 新規追加: `docs/getting-started.md`, `docs/dev_guide.md`, `docs/index.md`
- 既存更新: `README.md`（Quick Start の `cd rc02`、Amagi Protocol のリンク補正）
- 差分記録: `docs/diff_log/diff_docs_reorg_20250905.md`
- 削除は未実施（候補抽出のみ）。

## 削除候補（要レビュー）
- `docs/roles_assignment.md`: 役割情報は `AGENTS.md` に集約。重複は廃止候補。
- `docs/structure/readme.md`: 役割が README/索引と重複。統合または削除。
- `docs/chart.md` と `docs/chart_ut_checklist.md`: 内容が重複する場合は統合。

## 次アクション（第2弾: 体系化と削除実行）
1) `overview.md` と `AGENTS.md` の整合確認（見出し・用語・導線）
2) `docs/index.md` を基準に、主要ドキュメントの先頭へ“責務1行定義”を追記
3) リンク切れスキャン（`rg -n "\]\(.*\.md\)" -S`）と修正
4) 削除候補の最終判断（PM 合意 → diff_log 追記 → 実削除）
5) `features/**/instruction.md` が参照する関連 docs を `docs/index.md` に反映

## DoD（完了条件）
- README → `docs/index.md` の導線を確認できる
- 主要 docs に“責務1行定義”がある
- diff_log に「作成/修正/削除」が残っている
- 重大なリンク切れが 0 件

