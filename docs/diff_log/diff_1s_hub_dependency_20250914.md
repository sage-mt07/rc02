# diff: 1s ハブ依存の正規化（TABLE→STREAM）

- 日付: 2025-09-14
- 目的: BarDslExplain/派生DDLにおける 1s ハブの依存順を docs/chart.md（TABLE→STREAM）に合わせ、`BAR_1S_FINAL_S does not exist` エラーを解消。

## 変更点

- DerivationPlanner: 1s 最終テーブル（`*_1s_final`）の `InputHint` を `null` に変更し、元の入力ストリーム（例: `Rate/deduprates`）から直接 CTAS を生成するように修正。
- DerivationPlanner: 1s ハブ・ストリーム（`*_1s_final_s`）は 1s テーブルを参照するように維持（TABLE→STREAM の順序）。
- DerivedTumblingPipeline: `inputOverride` のデフォルトを `null` に変更し、`AdditionalSettings["input"]` がある場合のみ上書き。`Final1s` は元の `QueryModel.SourceTypes` による FROM を使用。
- KsqlCreateWindowedStatementBuilder: `1s` を `WINDOW TUMBLING (SIZE 1 SECONDS)` として正しく出力するように単位マッピングを拡張。

## 期待効果

- 生成される DDL が以下の順序になる:
  1. `CREATE TABLE <base>_1s_final AS ... FROM <source> ... EMIT FINAL;`
  2. `CREATE STREAM <base>_1s_final_s AS SELECT * FROM <base>_1s_final EMIT CHANGES;`
- `EXPLAIN`/実行時の `BAR_1S_FINAL_S does not exist` を解消。

## 影響範囲

- Tumbling 系の派生エンティティ生成（1s/ハブ）。
- その他の足（1m/5m など）は従来どおりハブ（`*_1s_final_s`）を参照。

## 移行・互換性

- 既存の 1s ハブ構成を再作成する場合、古い `*_1s_final`/`*_1s_final_s` は一度 DROP 後に再生成されます。

