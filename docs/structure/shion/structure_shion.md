# 詩音視点 テスト観点

[shared/structure_base.md](../shared/structure_base.md) を踏まえ、テストエンジニアである詩音の立場からレイヤーごとのテスト指針を記載します。

## テストレイヤー

| レイヤー        | 主な対象クラス例                 | テスト方針 |
|-----------------|----------------------------------|-----------|
| Application     | `KsqlContextBuilder`             | 統合テストで設定の組み合わせを確認 |
| Core            | `KsqlEntity`, `EntityModelBuilder`   | モデル構築のユニットテスト |
| Messaging       | `KafkaProducer`, `KafkaConsumer` | 擬似ブローカーを用いた送受信試験 |
| Serialization   | `AvroSerializer`                 | スキーマ整合性と例外ハンドリング |
| Cache      | `StreamizCache`              | データ永続化と復元の検証 |
| Window          | `WindowProcessor`                | 境界値・時間経過による動作確認 |

## 観測ポイント

- 各レイヤーで公開されるインターフェースをモック化し、失敗系を網羅する
- 外部依存がある場合はテストダブルを活用し、再現性を保つ
