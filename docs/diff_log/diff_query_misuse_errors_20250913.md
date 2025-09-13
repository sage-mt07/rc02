# diff: query_misuse_errors (2025-09-13)

目的: 誤用されやすいクエリ構築パターンで、開発者が即座に原因を掴めるエラーをUTで保証する。

追加UT:
- Select で集計と非集計の混在 (GROUP BYなし) を禁止し、明確なエラーメッセージを検証。
- WHERE 句での集計関数使用を禁止し、HAVING 利用を促すメッセージを検証。
- （既存）二重集計の禁止と、過剰な式の深さの検出をカバー。

ファイル:
- tests/Query/Builders/ValidationErrorTests.cs に追加。

影響範囲:
- テストのみ。プロダクションの挙動変更なし。

