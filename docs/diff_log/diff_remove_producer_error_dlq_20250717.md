# 差分履歴: remove_producer_error_dlq

🗕 2025年7月17日（JST）
🧐 作業者: codex

## 差分タイトル
Producer エラー時の自動 DLQ 送信を廃止

## 変更理由
送信エラー時に DLQ へ転送する処理が期待通り動作しないため、KsqlContext から自動送信ハンドラを削除し、利用側で必要に応じて DLQ を呼び出す方式へ戻す。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlContext` で `KafkaProducerManager.ProduceError` への登録を削除
- 自動 DLQ 送信は利用者実装に任せる設計へ変更

## 参考文書
- `docs/api_reference.md`
