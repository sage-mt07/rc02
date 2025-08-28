# 差分履歴: codex_must_read_agents

- 目的: Codex セッション開始時に `AGENTS.md` と `overview.md` を必読とする運用を明文化。
- 背景: エージェント運用の一貫性を高め、AGENTSガイドとリポジトリ全体の整合性を担保するため。

## 変更内容
- `codex.md` に「Startup Checklist (必読チェック)」を追加。
  - セッション開始時に `AGENTS.md`/`overview.md` を読むこと。
  - どちらかを更新した場合にもう一方の整合性を確認すること。
  - AGENTS憲章・diff_log・features 構造・エスカレーションの遵守を明記。

## 影響範囲
- Codex CLI を用いた作業開始手順の標準化。
- ドキュメント整合性チェックの明確化（AGENTS.md と overview.md）。

## フォローアップ
- 必要に応じて `AGENTS.md` の「現場入口ガイド」に本運用の文言を追加検討。
- 初回オンボーディング時のチェックリスト化（別PRで対応可）。
