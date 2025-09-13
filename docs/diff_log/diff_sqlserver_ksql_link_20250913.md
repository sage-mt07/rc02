# diff: sqlserver_to_kafka_guide_link (2025-09-13)

目的: 既存ガイドから関数/型対応表への参照リンクを追加し、利用者導線を改善。

変更:
- `docs/sqlserver-to-kafka-guide.md` 内の KSQL 関連セクション直後に参照リンクを追加。
  - 文字化け箇所があるため、衝突回避のため ASCII の小見出しと参照行を挿入。
  - `## KSQL function and type mapping` / `- See: docs/ksql-function-type-mapping.md`
- 参照先: `docs/ksql-function-type-mapping.md`

補足:
- ガイド本文の文字化けは別対応で UTF-8 正規化＋内容復元を推奨。
