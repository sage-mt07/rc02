# 差分履歴: toquerydsl_aggregate

🗕 2025年8月1日（JST）
🧐 作業者: codex

## 差分タイトル
ToQuery DSL 集約判定とCREATE文種別切替

## 変更理由
集約関数を使用した場合に`CREATE TABLE`を生成できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlQueryModel` に `IsAggregateQuery` プロパティを追加
- `KsqlQueryable2.Select` で集約関数を検出し `IsAggregateQuery` を設定
- `KsqlCreateStatementBuilder` が `CREATE TABLE` / `CREATE STREAM` を自動切替

## 参考文書
- `docs/test_guidelines.md`
