# 差分履歴: DLQ headers mapping

🗕 2025-08-10
🧐 作業者: assistant

## Excluded unsupported headers field
DlqEnvelope.Headers is ignored during schema registration.

## 変更理由
Dictionary types are not supported by KSQL schema generation, causing DLQ stream creation to fail.

## 追加・修正内容（反映先: oss_design_combined.md）
- Added KsqlIgnore to DlqEnvelope.Headers to skip unsupported Dictionary

## 参考文書
- `docs/changes/20250810_progress.md`
