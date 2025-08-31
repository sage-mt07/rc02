# features ディレクトリ運用ガイド

機能単位の作業はこの直下に `features/{機能名}/` を作成し、設計・実装・テスト・差分の成果物を集約します。

基本構成（推奨）
- instruction.md: 機能の目的、範囲、API・I/O、完了条件、担当、期日
- src/: 該当コード（既存コードにマージするまでの作業場）
- tests/: ユニット/物理テスト（最小再現/網羅の両輪）
- diff/: 当機能に関する補助的な差分や図表（正式な設計差分は docs/diff_log へ）

運用ルール
- 設計や仕様の確定/変更は `docs/diff_log/diff_{機能名}_{YYYYMMDD}.md` を必ず作成
- 日々の進捗・相談は `docs/changes/YYYYMMDD_progress.md` に追記
- PR前に instruction.md の完了条件に対するエビデンス（テスト、ドキュメント）をチェック

テンプレート
- `features/_template/instruction.md` をコピーし、機能名フォルダを作成して着手してください

