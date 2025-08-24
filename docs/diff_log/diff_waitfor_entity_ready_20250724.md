# 差分履歴: waitfor_entity_ready_api

🗕 2025年7月24日（JST）
🧐 作業者: assistant

## 差分タイトル
WaitForEntityReadyAsync API 追加

## 変更理由
- ストリーム/テーブル作成直後の伝搬遅延でクエリ失敗する問題を解消するため

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlContext` に `WaitForEntityReadyAsync<T>()` と `IsEntityReadyAsync<T>()` を実装
- ドキュメントに利用手順を追記
- Hello World サンプルを更新し、新API使用例を追加

## 参考文書
- `docs/getting-started.md`
