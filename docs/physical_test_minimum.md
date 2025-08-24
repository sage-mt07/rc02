# OSS物理テスト最小ガイド

## サブジェクト名ルール

Kafka/Schema Registry への Avro スキーマ登録時は、 **トピック名（小文字）-value** / **-key** の形式のみをサポートします。 
大文字を含むサブジェクト名は登録・参照ともにサポートされません。 

テストでは各テストが必要なスキーマを個別に登録します。このとき登録されるサブジェクト名もすべて小文字です。

## テスト実施時の注意点
- 送信メッセージは `Chr.Avro.Confluent` で自動生成した POCO スキーマを利用します。
- テーブルに対する `MIN` / `MAX` 集計は避け、STREAM クエリで動作を確認します。
- `GROUP BY` を含むクエリは Push Query (`EMIT CHANGES`) とし、Pull Query では実行しません。
- `WINDOW` 句は必ず `GROUP BY` 直後に記述してください。
- `CASE` 式では `THEN` と `ELSE` の型が一致しているかを確認します。


## ディレクトリ構成

物理テストは次の3カテゴリに分かれています。

- `physicalTests/Connectivity/` : Kafka や Schema Registry の疎通を確認する最小テスト群。
- `physicalTests/KsqlSyntax/` : API から生成された KSQL 文の構文チェック用テスト群。
- `physicalTests/OssSamples/` : LINQ DSL 等、OSS サンプルコードを使った統合テスト群。
