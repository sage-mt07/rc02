# 鳴瀬視点 構造概要

本ドキュメントは [shared/structure_base.md](../shared/structure_base.md) を参照し、実装担当AIである鳴瀬の観点からクラス間構造を整理したものです。

## 依存順

```
Application -> Core -> Messaging -> Serialization -> Cache -> Window -> Context
```

## 責務分離

| コンポーネント            | 主要クラス例                       | 概要                                   |
|---------------------------|------------------------------------|----------------------------------------|
| **Builder**               | `KsqlContextBuilder`               | DSL設定を集約し `KsqlContext` を生成 |
| **Pipeline**              | `QueryBuilder`                     | LINQ式を解析しKSQLへ変換             |
| **Context**               | `KsqlContext`, `KafkaContextCore`   | 実行時の設定・モデル構築を担当       |
| **Serializer**            | `AvroSerializer`, `SchemaGenerator`| Avroスキーマ生成とシリアライズ       |

## LINQ式ベース動作

1. 開発者は LINQ 拡張メソッドでクエリを記述します。
2. `QueryBuilder` が式ツリーから KSQL 文へ変換します。
3. `KsqlContextBuilder` が設定をまとめ、`KsqlContext` を生成します。
4. 生成されたコンテキストを通じて `KafkaProducer` / `KafkaConsumer` がメッセージ送受信を行います。
