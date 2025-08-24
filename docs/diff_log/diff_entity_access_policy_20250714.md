# 差分履歴: entity_access_policy

🗕 2025年7月14日（JST）
🧐 作業者: 天城

## 差分タイトル
Entity役割指定方法の統一

## 変更理由
OSS 全体で Entity の利用目的を明確化し、API 設計の一貫性を高めるため。

## 追加・修正内容（反映先: oss_design_combined.md）
- Entity 登録時の役割を `readonly` `writeonly` `readwrite` の3種類とする方針を追加
- Mapping/Messaging/KsqlContext など関連 API もこの役割判定で分岐する旨を追記
- K8s 用語を排除し、一般的な DB 用語で統一する指示を明示

## 参考文書
- `fluent_api_initial_design.md`
