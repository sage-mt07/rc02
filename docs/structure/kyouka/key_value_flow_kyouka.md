# Key-Value Flow Review (Kyouka View)

この文書は [shared/key_value_flow.md](../shared/key_value_flow.md) を参照し、設計監査担当の鏡花が最新の構造を評価したものです。

## 構造上のポイント

- **責務集中**: `KsqlContext` がProduce/Consumeを統括する一方、シリアライズ処理は `AvroSerializer` に限定されています。役割の境界が明確で、拡張や差し替えが容易です。
- **依存順序**: Query → Context → Messaging → Serialization → Kafka の一方向依存で、逆参照はありません。
- **結合度**: Messaging と Serialization はインターフェース経由で連携し、具象実装を隠蔽しています。

## 再整理に向けたコメント

| 指摘項目 | コメント |
|---------|---------|
| コンテキスト | `KsqlContextBuilder` のオプションが増える可能性が高いため、設定クラスを分割して責任範囲を絞るべきです |
| パイプライン | `QueryBuilder` に処理が集中しているため、式解析・Key生成・Serialize呼び出しを分割する案を検討してください |
| テスト観点 | ミドルウェア層での失敗系テストが不足しがち。詩音のテスト計画と連携して補完すること |

設計変更や責務整理の提案は `docs/diff_log/` に記録し、チームで共有してください。

## 2025-07-22 監査コメント

### MappingManager設計粒度
- EntityModel 単位での登録のみでは検証手段が限られる。登録済みモデル一覧を取得する API を追加し、互換性チェックを容易にする。
- `Register<TEntity>()` の連続呼び出しによる状態変化を抑えるため、固定化オプションを検討する。

### 公開APIと拡張性
- `Register<TEntity>()` は自身を返すフルエントAPI形式も許容されるとメソッドチェーンが可能。
- `ExtractKeyValue<TEntity>()` では KeyExtractor を外部注入できるようにして、独自ルール拡張に備える。

### 責任境界の整理
- **KsqlContextBuilder**: 構成情報の集約と依存サービス生成のみ担当し、エンティティ登録は MappingManager へ委譲する。
- **QueryBuilder**: LINQ 式解析から QuerySchema 生成までに責任を限定し、key/value 生成や Kafka 送信処理は持たない。
- **MappingManager**: POCO ↔ key/value 変換ロジックを保持し、Kafka や Query 解析への依存を避ける。

### 越境リスクと注意点
- QueryBuilder が MappingManager の内部辞書へ直接アクセスするとテストが困難になる。インターフェース越しの連携に留めること。
- KsqlContextBuilder が QueryBuilder の状態を変更すると Builder の再利用性が下がるため、設定オブジェクトを明確に分離する。
- 各層で Fail-Fast ポリシーを徹底し、未登録モデル利用時は即例外を投げる。

### 型情報管理レビュー
- `PropertyMeta` による型情報の集中管理を正式ルールとし、Mapping 以外の層で型定義を持たないことを確認。
- Messaging 層では `KafkaProducerManager` と `KafkaConsumerManager` が Avro 変換とキャッシュ処理を担う。Mapping 更新時の影響を最小化しつつ、送受信効率を保つ。
- 新しい POCO 追加時は必ず MappingManager 登録フローが実行されているかを監査ポイントとする。
