# 差分履歴: TimeFrame rename

🗕 2025-08-24
🧐 作業者: naruse

## TimeFrame replaces BasedOn DSL
Tumbling スケジュール結合 DSL `BasedOn` を `TimeFrame` にリネームしました。

## 変更理由
名前をフレーム表現に統一するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlQueryable.TimeFrame` を追加し旧 `BasedOn` を置き換え
- テストとドキュメントを TimeFrame 呼び出しへ更新

## 参考文書
- `docs/chart.md`
