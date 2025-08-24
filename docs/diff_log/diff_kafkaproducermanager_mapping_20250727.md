# 差分履歴: kafkaproducermanager_mapping

🗕 2025年7月27日（JST）
🧐 作業者: assistant

## 差分タイトル
KafkaProducerManager が MappingRegistry を利用して key/value 分割送信に対応

## 変更理由
キー付きエンティティ送信時に POCO を直接シリアライズしていたため、動的に生成された key/value 型を通じて正確な Avro スキーマを適用するよう修正。

## 追加・修正内容（反映先: oss_design_combined.md）
- `GetProducerAsync` で `KeyValueTypeMapping` を参照し、key プロパティの有無で Producer を切り替えるよう更新。
- `SendAsync`/`DeleteAsync` が `PopulateKeyValue` を使って key/value オブジェクトを生成してから送信するよう改修。
- 新たなテスト `KafkaProducerManagerMappingTests` を追加し、動的型利用を検証。

## 参考文書
- `docs/architecture/key_value_flow.md` セクション 8
