# 差分履歴: dlq_envelope_mapping

🗕 2025年8月11日（JST）
🧐 作業者: naruse

## 差分タイトル
Messaging.DlqEnvelope を MappingRegistry に登録

## 変更理由
DlqProducer が DlqEnvelope のマッピング未登録で失敗するため。

## 追加・修正内容（反映先: src/KsqlContext.cs）
- InitializeEntityModels で Messaging.DlqEnvelope を登録

## 参考文書
- なし
