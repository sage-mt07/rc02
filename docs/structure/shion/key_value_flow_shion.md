# Key-Value Flow Testing (Shion View)

この文書は [shared/key_value_flow.md](../shared/key_value_flow.md) と
[naruse/key_value_flow_naruse.md](../naruse/key_value_flow_naruse.md) を参照し、
テスト担当の詩音がどのレイヤーをどう検証するかをまとめたものです。

## テストレイヤーごとの焦点

| レイヤー        | 対象クラス例                  | 主なテスト内容 |
|-----------------|------------------------------|----------------|
| Query           | `EntitySet<T>`               | LINQ式からのKey生成の妥当性 || Context         | `KsqlContext`, `KsqlContextBuilder` | オプション設定と依存注入の組み合わせ |
| Messaging       | `KafkaProducer`, `KafkaConsumer` | トピック送受信時の例外処理とリトライ確認 |
| Application  | Consumer Handler       | ハンドラ呼び出しと後処理の検証 |
| Serialization   | `AvroSerializer`, `AvroDeserializer` | スキーマ互換性とエラー時の挙動 |
| Kafka           | テストブローカー             | 実際の配信確認（統合試験） |

## 観測ポイントとアプローチ

1. 各レイヤーはモックを用いたユニットテストを基本とし、外部依存はテストダブルで置き換えます。
2. Pipeline全体は統合テストとして `KsqlContext` を経由した end-to-end シナリオを実行します。
3. `MappingManager` のマッピング定義が期待どおりに適用されるかを確認します。
4. エラー発生時にはどのレイヤーでハンドルされるかをログと例外種別で検証します。

テスト結果や不足分は `tests/` 以下へ追加し、レビュー担当の鏡花とも共有してください。

### MappingManager Test Patterns

#### Unit Tests
- EntityModel 登録後に `ExtractKeyValue` が正しい key/value を返す
- 未登録エンティティを渡した場合に例外をスローする
 - 複合キー定義 (`[KsqlKey]` で複数プロパティ指定) の抽出結果
- null エンティティ引数を渡したときの振る舞い
- 同一型を重複登録した際の上書き挙動

#### Integration Tests
- `KsqlContext` との連携で key/value 抽出後に Kafka へ送信できる
- Mapping 定義のない型を使用した場合にパイプライン全体で失敗を検知できる

## Mappingテスト追加方針
sharedの新セクションで定義された型情報管理フローを踏まえ、以下を重点的に検証します。
1. `PropertyMeta` から生成される KeyType/ValueType が期待どおりか。
2. MappingManager 更新時に Messaging 層への影響がないことを統合テストで保証。
3. 新規POCO追加時に登録漏れが発生した場合のFail-Fast動作。

エッジケースおよび失敗系は `tests/Mapping/MappingManager_viewpoints.md` に詳しく記述します。
