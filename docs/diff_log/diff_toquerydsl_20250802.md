# 差分履歴: toquerydsl_executionmode

🗕 2025年8月2日（JST）
🧐 作業者: codex

## 差分タイトル
ToQuery DSL 実行モード制御と順序チェック追加

## 変更理由
Push/Pull 実行モードを指定可能にし、チェーン順序の誤使用を防ぐため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlQueryModel` に `ExecutionMode` プロパティを追加
- `.AsPush()` `.AsPull()` により実行モードを指定
- `KsqlCreateStatementBuilder` が ExecutionMode に応じて `EMIT CHANGES` を付与/省略
- `Tumbling()` は未対応エラーとする仮実装
- Join/Where/Select の呼び出し順を検証する例外処理を強化

## 参考文書
- `docs/test_guidelines.md`

## 追加修正 2025-08-02
- 日本語メッセージのエスケープを解除し、UTF-8 文字列を直接使用。
