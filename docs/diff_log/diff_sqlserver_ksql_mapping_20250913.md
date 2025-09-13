# diff: sqlserver_ksql_mapping (2025-09-13)

目的: SQL Server → ksqlDB の関数/データ型対応を整理し、移行時の指針を提供。

追加:
- `docs/ksql-function-type-mapping.md` を新規作成。
  - データ型の対応、主要関数（文字列/数値/日付/集計/JSON/条件/変換）の対応表を掲載。
  - 注意点（改行差、DECIMAL 精度、集計混在禁止、WHERE 集計禁止、DATEDIFF/FORMAT 非対応の補完方針）を記載。

既存ガイドへの組み込み:
- `docs/sqlserver-to-kafka-guide.md` から参照リンクを追加する想定（別PR/差分で追従）。

