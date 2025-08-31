# diff_join_within_20250831

- Stream×Stream JOIN に `.Within(seconds)` を要求するテストを追加。
- `WITHIN <n> SECONDS` の SQL 出力と未指定時の例外を検証。
- SELECT/ON の未修飾カラム参照を例外として検出し、出力をフル修飾＋バッククォートに統一。
