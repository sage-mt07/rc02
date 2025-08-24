# 差分履歴: producer_error_event

🗕 2025年7月14日（JST）
🧐 作業者: codex

## 差分タイトル
Producer エラー通知をイベント化

## 変更理由
Messaging から DLQ 送信責務を分離するため、Producer 側もイベントを通じてエラーを外部へ通知する方式に統一。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KafkaProducer` に `SendError` イベントを追加
- `KafkaProducerManager` で `ProduceError` として集約し、`KsqlContext` が DLQ 送信ハンドラを登録
- API ドキュメントに Producer エラーイベントを追記

## 参考文書
- `docs/api_reference.md`
