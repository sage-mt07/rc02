# 差分履歴: mapping_fixes

🗕 2025年7月28日（JST）
🧐 作業者: assistant

## 差分タイトル
Mapping 対応に伴うテスト修正と単純コンシューマ改善

## 変更理由
前回実装した KafkaProducerManager/KafkaConsumerManager の変更により
テストが内部フィールド型の変更に追随できず失敗していた。
また SimpleKafkaConsumer が無限ループとなる問題もあったため修正。

## 追加・修正内容（反映先: oss_design_combined.md）
- テスト側で `_producers` `_topicProducers` フィールドの型を
  `ISimpleKafkaProducer` 用の辞書として参照するよう更新
- SimpleKafkaConsumer で `Consume` が null を返した場合は
  ループを終了するよう変更
- キーレスProducer生成時のKeyシリアライザを削除し、
  Schema Registry 未設定でも利用可能に

## 参考文書
- `docs/changes/20250727_progress.md`
