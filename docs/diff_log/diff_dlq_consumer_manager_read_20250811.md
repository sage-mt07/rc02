# 差分履歴: dlq_consumer_manager_read

🗕 2025年8月11日（JST）
🧐 作業者: assistant

## 差分タイトル
DlqClient.ReadAsync を KafkaConsumerManager 利用方式へ変更

## 変更理由
DLQ 読み取り処理を KafkaConsumerManager に統一し、Avro POCO 処理を共通化するため。

## 追加・修正内容（反映先: src/Core/Dlq/DlqClient.cs, src/KsqlContext.cs）
- DlqClient が KafkaConsumerManager を受け取り ReadAsync で ConsumeAsync<DlqEnvelope> を使用
- KsqlContext で DlqClient を KafkaConsumerManager 経由で初期化

## 参考文書
- なし
