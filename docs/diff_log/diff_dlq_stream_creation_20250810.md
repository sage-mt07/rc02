# 差分履歴: DLQ stream creation

🗕 2025-08-10
🧐 作業者: 詩音（品質監査AI）

## DLQ stream now auto-created
DlqEnvelope is processed during schema registration so its stream is generated.

## 変更理由
DLQ topic existed but stream was missing because schema registration skipped DlqEnvelope.

## 追加・修正内容（反映先: oss_design_combined.md）
- Removed DlqEnvelope exclusion in RegisterSchemasAndMaterializeAsync

## 参考文書
- `docs/changes/20250810_progress.md`
