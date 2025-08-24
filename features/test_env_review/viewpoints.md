# Test Env Review 観点リスト

## 基本方針
- 物理テスト実行前後で環境を完全に初期化する
- ksqlDB/Kafka/SchemaRegistry の疎通確認を必須とする
- DLQ topic が存在しない場合は自動生成する
- 既存スキーマが古い場合はサブジェクトを削除してから再登録する
- Push/Pull query の区別、`EMIT CHANGES` の有無を検査する
- サポート外関数を含むクエリは自動でスキップ

