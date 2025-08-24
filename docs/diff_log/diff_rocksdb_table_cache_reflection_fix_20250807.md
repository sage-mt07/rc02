# 差分履歴: RocksDbTableCache reflection fix

🗕 2025-08-07 (JST)
🧐 作業者: assistant

## 差分タイトル
TargetInvocationException をラップしている InvalidStateStoreException をリトライ可能にする

## 変更理由
リフレクションによるストア呼び出しで発生する TargetInvocationException によりリトライ処理が働かず、テストが失敗していたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- TargetInvocationException から InnerException の InvalidStateStoreException を抽出しリトライを継続

## 参考文書
- docs_advanced_rules.md のエラーハンドリング方針
