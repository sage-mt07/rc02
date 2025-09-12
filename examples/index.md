# Examples Index (Consolidated)

このフォルダはサンプルを次の方針で統合しました。従来パスは段階的に非推奨化します（各フォルダの `DEPRECATED.md` を参照）。

- basics: 最小の送受信（hello-world を統合）
- configuration: appsettings と Builder/属性マッピングの最小例（configuration, configuration-mapping を統合）
- error-handling: Retry/OnError/DLQ を単一サンプルで切替（error-handling, error-handling-dlq を統合）
- query-basics: フィルタと View 相当の ToQuery を併載（query-filter, view-toquery を統合）
- windowing: tumbling live と 1m→5m ロールアップ検証を併載（必要に応じて入門/検証に分割）
- advanced: daily-comparison（高度シナリオ）と oss-bars-verify（OSS検証）

共通の起動コマンド
- `docker-compose -f tools/docker-compose.kafka.yml up -d`

---

## Consolidated Entrypoints

- basics/ … 最小POCO + 送受信（従来: basic-produce-consume, hello-world）
- configuration/ … appsettings + Builder/属性（従来: configuration, configuration-mapping）
- error-handling/ … Retry/OnError/DLQ 切替（従来: error-handling, error-handling-dlq）
- query-basics/ … `.Where(...)` と View/ToQuery（従来: query-filter, view-toquery）
- windowing/ … WhenEmpty/Tumbling/1m→5mロールアップ（従来: tumbling-live-consumer, rollup-1m-5m-verify）
- advanced/ … daily-comparison, oss-bars-verify

補助サンプル（個別テーマ）
- headers-meta/ … ヘッダー付与と受信メタの利用
- schema-attributes/ … [KsqlKey], [KsqlDecimal], [KsqlTimestamp] の典型例
- manual-commit/ … autoCommit:false + Commit()
- table-cache-lookup/ … `[KsqlTable]` とキャッシュの利用
- whenempty-schedule/ … WhenEmpty スケジュールの入門例
