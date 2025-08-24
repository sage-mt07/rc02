# 差分履歴: consume_from_beginning

🗕 2025-08-11 (JST)
🧐 作業者: naruse

## ConsumeAsync FromBeginning parameter
KafkaConsumerManager.ConsumeAsyncにFromBeginning引数を追加し、ResetOffsetsToBeginningを廃止。

## 変更理由
DLQ読み取り時に専用IFを介さず先頭からの消費を行えるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- ConsumeAsyncがfromBeginning引数を受け取り、内部でオフセットを0にコミット
- ResetOffsetsToBeginning<T>を削除
- DlqClient.ReadAsyncが新引数を利用

## 参考文書
- `docs/changes/20250811_progress.md`
