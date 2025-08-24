# 差分履歴: rename_baseon_method

🗕 2025年7月31日（JST）
🧐 作業者: naruse

## 差分タイトル
BaseOn メソッド名を BasedOn に変更

## 変更理由
API 名とドキュメントの整合性を保つため。機能自体は変わらないが名称を過去形に統一した。

## 追加・修正内容（反映先: oss_design_combined.md）
- `ScheduleWindowBuilder` の公開メソッド `BaseOn` を `BasedOn` にリネーム
- 内部実装 `BaseOnImpl` を `BasedOnImpl` へ変更
- テストおよびドキュメント内の利用箇所を更新

## 参考文書
- `docs/api_reference.md`
