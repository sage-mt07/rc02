# 差分履歴: TimeFrame order

🗕 2025-08-24
🧐 作業者: assistant

## TimeFrame precedes Tumbling
TimeFrame 呼び出し後にしか Tumbling を許可しないよう DSL に `IScheduledScope` を追加し、`TimeFrame().Tumbling()` の順序を型で拘束しました。

## Fallback behavior clarified
TimeFrame 未使用時の Day/Week/Month の UTC 解釈と、dayKey 指定時に Week/Month が営業日集合から導出されることを文書に追記しました。
