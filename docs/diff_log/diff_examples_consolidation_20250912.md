# examples 統合差分 20250912

目的: サンプルを機能軸で統合し、重複を排除。利用者導線を簡素化。

新構成（論理エントリ）
- examples/basics … 最小の送受信（hello-worldを統合）
- examples/configuration … appsettings + Builder/属性（configuration, configuration-mappingを統合）
- examples/error-handling … Retry/OnError/DLQ 切替（error-handling, error-handling-dlqを統合）
- examples/query-basics … フィルタ + View/ToQuery（query-filter, view-toqueryを統合）
- examples/windowing … tumbling live + 1m→5m ロールアップ（tumbling-live-consumer, rollup-1m-5m-verifyを統合）
- examples/advanced … daily-comparison, oss-bars-verify

非推奨マーカー追加（移行先）
- examples/hello-world → basics
- examples/configuration-mapping → configuration
- examples/error-handling-dlq → error-handling
- examples/query-filter → query-basics
- examples/view-toquery → query-basics
- examples/rollup-1m-5m-verify → windowing

備考
- 現時点ではコード移設は最小限。新フォルダにREADMEを配置し、移行先を明示。
- 追って各プロジェクトのcsproj/コードを新フォルダへ移設し、旧フォルダは削除予定。

