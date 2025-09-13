# diff: sqlserver_guide_utf8_restore (2025-09-13)

目的: 文字化けしたガイド本文を段階的にUTF-8で復元（目次、Streams vs Tables、Pull/Push、概要）。

変更:
- `docs/sqlserver-to-kafka-guide.md`
  - 目次を再構成（概要、KSQL DDL/Avro、Streams vs Tables、Pull vs Push、まとめ）。
  - 「概要」セクションを新規整備（移行時の要点を箇条書き）。
  - 「Streams vs Tables」を日本語で復元（定義と設計の要点）。
  - 「Pull vs Push クエリ」を日本語で復元（定義とSQL例）。
  - 既存の文字化けテーブル残滓を適宜削除し、重複見出しを整理。

未対応（今後の候補）:
- 「エコシステム連携とセキュリティ」「運用/監視」「移行ステップ」など残りの文字化け章の復元。
- 目次アンカーの完全整合と内部リンクの確認。
