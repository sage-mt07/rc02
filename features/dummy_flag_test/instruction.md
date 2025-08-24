詩音へ
タイトル:
「曳光弾（ダミーフラグ付きメッセージ）の投入による KSQL スキーマ確定テスト」

本文:

目的
Kafka Producer から AVRO 形式でダミーデータを送信する際、Kafka メッセージのヘッダーに is_dummy=true のフラグを付与した状態で送信し、KSQL（またはconsumer側）がスキーマを認識できるか、かつ「本番データと区別できるか」をテストする。

要件

- 対象テーブル（例: orders, customers）ごとに最低1件の「ダミーフラグ付き」レコードを投入
- ダミーフラグはKafkaメッセージヘッダー（例: is_dummy もしくは is_tracer などで、boolまたはstring型でtrueをセット）
- consumer側またはKSQLでダミーデータの判別が可能であること
- DMLクエリ（SELECT等）がエラーにならないこと（column 'REGION' cannot be resolved等が出ないこと）

実装手順案

1. Producer（C#/.NET）のサンプルで、ヘッダー付きメッセージ送信を追加
2. 既存のテスト用DDL（orders/customers等）で本挙動を確認
3. 投入直後にSELECTなどでスキーマエラーが発生しないことを確認
4. consumer側でヘッダー値を参照し、ダミーレコードかどうか判定するサンプルも作成

備考:

従来 getting-started.md に記載していた「ダミーデータ投入」に、このフラグ方式を標準化するかどうかも含め、出力・検証をお願いします。
