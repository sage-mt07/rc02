# diff: sqlserver_guide_hazuki_rewrite (2025-09-13)

目的: SQL Server 技術者向けに `docs/sqlserver-to-kafka-guide.md` を葉月スタイルで再構成し、Retention / latest / earliest の要点を追記。

変更:
- 1行サマリを先頭に追加。
- できること/5つの要点/Streams vs Tables/Pull vs Push/スキーマとDECIMAL/リンク集/チェックリスト構成へ全面整理。
- Retention（保持期間・再生成設計）、latest/earliest（初期位置の使い分け）を明記。
- 内部仕様の詳細は避け、実務で必要な最小限の言葉で記載。

影響範囲:
- ドキュメントのみ。コード変更なし。

