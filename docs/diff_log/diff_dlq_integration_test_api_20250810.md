# 差分履歴: DLQ Integration Test Update

🗕 2025-08-10 (JST)
🧐 作業者: 鳴瀬

## DlqIntegrationTests 新API使用
DLQ統合テストを `ctx.Dlq.ReadAsync` で読み取る形に変更。

## 変更理由
- 新設DLQ Read APIの利用例を反映するため

## 追加・修正内容（反映先: oss_design_combined.md）
- `physicalTests/OssSamples/DlqIntegrationTests.cs` を更新

## 参考文書
- `physicalTests/OssSamples/DlqIntegrationTests.cs`
