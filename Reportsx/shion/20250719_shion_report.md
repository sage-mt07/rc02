# 2025-07-19 詩音レポート
- `examples/daily-comparison` サンプルを Kafka.Ksql.Linq ベースに改修。
- RateSender で1秒おきに100件送信し日次集計を実行。
- 比較結果取得用の ComparisonViewer を整備。
- 自動テストでは集計ロジックと送信処理を検証。
