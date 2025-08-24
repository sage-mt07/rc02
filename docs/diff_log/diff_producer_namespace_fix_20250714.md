# 差分履歴: producer_namespace_fix

🗕 2025年7月14日（JST）
🧐 作業者: codex

## 差分タイトル
Producer 例外名前空間の衝突修正

## 変更理由
Producers.Exception 名前空間が System.Exception と競合しビルドエラーとなったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- フォルダ名を `Producers/Exception` から `Producers/Exceptions` へ変更
- `KafkaProducerManagerException` の名前空間を更新
- 参照していたテストコードの using を修正
- `KsqlContext` の ProduceError ハンドラを Task を返す形に修正

## 参考文書
- `docs/api_reference.md`
