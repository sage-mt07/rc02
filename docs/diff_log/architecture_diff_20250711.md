# 差分履歴: architecture_restart_20250711

🗕 2025年7月11日（JST）
🧐 作業者: 鏡花（品質監査AI）

## 概要
`docs/architecture_restart_20250711.md` で提示された再構築方針と、現行リポジトリの実装を比較した。
主な対象は Messaging、Serialization、Core、ProducerPool、KsqlContext、Configuration、Query の各領域である。

---

## Messaging

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| Messaging namespace | 型安全Producer/ConsumerとManagerにより管理。`ProducerPoolException`のみ残存。 | 独自Pool関連の残骸が存在し、Confluent統合パターンが不明瞭。 | 🟨 | 旧Pool関連ファイルの整理、Confluent利用手順を明文化 | 鳴瀬 |

---

## Serialization

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| Serialization namespace | `Abstractions`, `Avro` 各サブ層に分割し独自マネージャを提供 | Confluent AvroSerializer 公式採用へ統合が必要 | 🔄 | Confluent 依存へ集約し、不要な独自管理層を段階的に削除 | 鳴瀬 |

---

## Core

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| Core namespace | `EntityModelBuilder.HasKey` によるキー指定へ移行済み。サンプル・ドキュメントも更新し、旧 `KeyAttribute` 参照を削除。 | LINQ式を前提としたAPI設計ドラフトを追加し、属性依存コードの廃止範囲を整理。 | 🟩 | 旧属性型を廃止予定として一覧化し、新API案を `docs/core_namespace_redesign_plan.md` にまとめた | 広夢 |

---

## ProducerPool

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| ProducerPool関連 | 実装は削除済みだが `ProducerPoolException` などファイルが残っている | 再設計では Pool 管理を廃止するため不要 | ⛔ | 例外含む関連ファイルを削除し、新構成へ整理 | 鳴瀬 |

---

## KsqlContext

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| KsqlContext | コンストラクタ内で SchemaRegistry クライアントや Serializer を保持し、接続管理を担う | 再構築では Key/Value 管理や Serializer バインド責務の見直しが求められている | 🟨 | 役割を Builder/Manager へ委譲し、Context 本体は実行時の統合のみ担当 | 鳴瀬 |

---

## Configuration

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| Configuration namespace | Messaging/Serialization 双方から設定クラスが分散配置 | 再構築方針では namespace 再配置と責務整理を想定 | 🟨 | Kafka 接続設定を Application 側へ集約し、共通オプションを一本化 | 鳴瀬 |

---

## Query（維持対象）

| 構造名 | 現在の状態 | 指摘された課題 | 対応区分 | 対応方針 | 担当AI |
|--------|-------------|----------------|----------|-----------|--------|
| Query namespace | LINQ式解析からKSQL生成までを担う。現行実装とドキュメントは整合。 | 現時点で大きな問題なし | 🟩 | 既存実装を維持し、他構造変更に合わせてインターフェースのみ確認 | 鳴瀬 |

---

以上より、各構造の修正・統合ポイントを整理した。今後の実装変更時はこの表を参照し、`docs/architecture_restart_20250711.md` の方針との整合を確認すること。
