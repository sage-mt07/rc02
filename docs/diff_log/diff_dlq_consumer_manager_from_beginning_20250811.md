# 差分履歴: dlq_consumer_manager_from_beginning

🗕 2025年8月11日（JST）
🧐 作業者: assistant

## 差分タイトル
DlqClient.ReadAsync で FromBeginning オプションを評価するよう修正

## 変更理由
DLQ 再読込時にオフセットを先頭へ戻すため。

## 追加・修正内容（反映先: src/Core/Dlq/DlqClient.cs）
- FromBeginning 指定時に ConsumerGroup のオフセットを先頭へコミットしてから KafkaConsumerManager で読み込み開始

## 参考文書
- なし
