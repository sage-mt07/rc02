# 差分履歴: DLQ OnError

🗕 2025-08-10 JST
🧐 作業者: naruse

## 差分タイトル
ForEachAsync handles action exceptions with DLQ routing

## 変更理由
- `.OnError(ErrorAction.DLQ)` had no effect when the processing action threw

## 追加・修正内容（反映先: oss_design_combined.md）
- catch exceptions in `EventSet.ForEachAsync` and invoke error sink
- expose `ConsumeAsync` for test injection
- add regression test ensuring DLQ sink receives failures

## 参考文書
- `docs/docs_advanced_rules.md` セクション 5.1
