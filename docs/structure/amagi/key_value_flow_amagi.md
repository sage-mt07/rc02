# Key-Value Flow Management (Amagi View)

この文書は [shared/key_value_flow.md](../shared/key_value_flow.md) を参照し、PMである天城がフロー全体の進行管理と依存関係を整理したものです。

## 依存関係と階層

```
Query
      ↳ KsqlContext
          ↳ Messaging
              ↳ Serialization
                  ↳ Kafka
```

1. **Query**: DSL入力とLINQ式を担当。最初に仕様変更の影響を受ける。
2. **KsqlContext**: 構成情報を集中管理し、Pipeline初期化を行うハブ。
3. **Messaging**: Kafka とのやり取りを抽象化。プロデューサ/コンシューマの両責任を持つ。
4. **Serialization**: Avroスキーマの生成と変換。互換性維持が重要なため、更新は慎重に。
5. **Kafka**: 実行基盤。外部依存のためテスト環境と本番環境の切り替えを明確化する。

## 優先度マップ

MappingManager の登録処理は KsqlContext 初期化と同時にまとめて実施する。
| 項目 | 優先度 | 管理方針 |
|-----|-------|---------|
| スキーマ互換性 | 高 | 変更時は必ず `diff_log` に記録し、全チームでレビュー |
| マッピング整合性 | 高 | `MappingManager` の定義変更は PM 承認のもとで実施 |
| メッセージ再試行 | 中 | `KafkaProducer` のリトライ設定を共有し、障害時の影響を最小化 |
| Context拡張 | 中 | 新しいオプション追加は `KsqlContextBuilder` に集約し、ドキュメントを更新 |
| テスト環境 | 高 | 詩音と協力し、Kafkaブローカーの模擬環境を常に整備 |

全体の進捗と課題は `docs/changes/` に記録し、週次でレビューします。

## 型情報管理とMessagingの分離

shared版で追加された [型情報・設計情報管理フロー](../shared/key_value_flow.md#8-%E5%9E%8B%E6%83%85%E5%A0%B1%E3%83%BB%E8%A8%AD%E8%A8%88%E6%83%85%E5%A0%B1%E7%AE%A1%E7%90%86%E3%83%95%E3%83%AD%E3%83%BC) を踏まえ、PM視点では次の点を管理指針とします。

1. **PropertyMeta集中管理**: Fluent APIで確定した型情報を `PropertyMeta` として集約し、変更時はMappingManager経由でのみ更新する。
2. **Messaging層の責務**: `KafkaProducerManager` と `KafkaConsumerManager` が POCO と key/value の Avro 変換を行う。生成した `Serializer`/`Deserializer` はキャッシュし、送受信性能を確保する。
3. **設計進化時の通知**: 新規POCO追加や属性変更はMapping登録フローの更新を必須とし、進捗ログに記録する。

この方針に沿って進捗管理・レビューを行います。
