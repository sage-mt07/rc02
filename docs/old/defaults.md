# 既定値まとめ

本ドキュメントでは `Kafka.Ksql.Linq` ライブラリにおける主な既定値を一覧形式で示します。設定ファイル生成やGUI確認支援の基礎資料として利用してください。

## Kafkaトピック構成

| 設定項目 | 既定値 | 説明 |
|---|---|---|
| PartitionCount | 1 | `TopicAttribute` でのトピック作成時パーティション数 |
| ReplicationFactor | 1 | トピックレプリカ数 |
| RetentionMs | 604800000 (7日) | メッセージ保持期間 |
| CleanupPolicy | delete | `Compaction=false` の場合のポリシー |
| MinInSyncReplicas | null | 未指定時はKafka既定に従う |

## Producer/Consumer構成

| 設定項目 | 既定値 | 説明 |
|---|---|---|
| Producer.Acks | All | 書き込み確認レベル |
| Producer.CompressionType | Snappy | 圧縮形式 |
| Producer.EnableIdempotence | true | 冪等性有効化 |
| Consumer.AutoOffsetReset | Latest | 初回読み込み位置 |
| Consumer.EnableAutoCommit | true | 自動コミット使用 |
| Consumer.AutoCommitIntervalMs | 5000 | コミット間隔 (ms) |

`ErrorHandlingContext` では `ErrorAction=Skip`, `RetryCount=3`, `RetryInterval=1秒` が既定です。

## DLQ構成

| 設定項目 | 既定値 | 説明 |
|---|---|---|
| DlqTopicName | "dead-letter-queue" | 共通DLQトピック名 |
| RetentionMs | 5000 | DLQ保持期間 (ms) |
| NumPartitions | 1 | DLQトピックのPartition数 |
| ReplicationFactor | 1 | DLQトピックのレプリカ数 |
| EnableAutoCreation | true | 起動時にDLQトピックを自動生成 |
| AdditionalConfigs.cleanup.policy | delete | 追加設定の例 |

## Window関連

| 設定項目 | 既定値 | 説明 |
|---|---|---|
| WindowType | Tumbling | ウィンドウ種別 |
| GracePeriod | 3秒 | 遅延許容時間 |
| OutputMode | Changes | `EMIT` モード |
| UseHeartbeat | true | Heartbeatトピック利用 |

## LINQ DSLの暗黙動作

- `ToList`/`ToListAsync` は Pull Query として実行されます【F:src/Query/Pipeline/DMLQueryGenerator.cs†L27-L34】。
- `WithManualCommit()` を指定しない `ForEachAsync()` は自動コミットで `T` を返します【F:docs/old/manual_commit.md†L1-L23】。
- `OnError(ErrorAction.DLQ)` を付与するとエラー時に共通DLQへ送信します【F:docs/old/defaults.md†L604-L615】。
- `UseFinalized()` を指定しない場合、`ToListAsync()` は通常トピックから読み取ります。
  `KsqlDslOptions.ReadFromFinalTopicByDefault` を `true` にすると既定で Final トピックを使用します。

更新時はこのファイルを基点に整合性を保ってください。
