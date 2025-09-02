# join_within / instruction

## 目的・背景
- Stream×Stream JOIN に `.Within(seconds)` を導入する。

## スコープ
- 含む: DSL と SQL 生成のテスト
- 含まない: トピック辞書や命名規約

## 成果物・完了条件
- [x] テスト: `.Within` を指定した JOIN の出力とバリデーションを確認
- [ ] ドキュメント: n/a
- [x] diff_log: `docs/diff_log/diff_join_within_20250831.md`

