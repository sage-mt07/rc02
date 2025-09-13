# Windowing（統合）

目的: TUMBLING/HOPPING/SESSION の基礎に加え、
- ライブ集計（Push, EMIT CHANGES）
- 1分→5分ロールアップ
を一箇所で確認できるよう統合しました。

統合対象
- 旧 `examples/tumbling-live-consumer`
- 旧 `examples/rollup-1m-5m-verify`

前提
- .NET 8 / Docker
- Kafka + Schema Registry + ksqlDB を起動（`docker-compose -f tools/docker-compose.kafka.yml up -d`）

手順（最小）
1) OnModelCreating で時間窓と集計を定義（参考: `docs/onmodelcreating_samples.md#7-時間窓（tumbling-1分push）`）
2) サンプルデータを投入（任意の Producer で OK。`examples/basic-produce-consume` でも可）
3) Push クエリ（EMIT CHANGES）でライブ結果を確認
4) 1分集計の上に 5分ロールアップを定義し、値の整合を確認

補足
- Streams/Tables と Pull/Push の概念図は `docs/sqlserver-to-kafka-guide.md` を参照
- ksqlDB の関数/型対応: `docs/ksql-function-type-mapping.md`
