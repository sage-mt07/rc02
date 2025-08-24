# 差分履歴: DLQ Read API

🗕 2025-08-10 (JST)
🧐 作業者: 鳴瀬

## IKsqlContext.Dlq.ReadAsync追加
DLQトピックからAvroメッセージを読み取り、RawTextを生成するAPIを追加。

## 変更理由
- DLQの内容を逐次確認するため

## 追加・修正内容（反映先: oss_design_combined.md）
- IKsqlContextに `Dlq` プロパティを追加
- `DlqClient` を新規実装し `ReadAsync` を提供
- `api_reference.md` にDLQ Read API節を追加

## 参考文書
- docs/api_reference.md
