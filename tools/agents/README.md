# Agents Presets

Codex CLI で役割ごとのペルソナを即座に呼び出せるよう、プリセットを用意しました。

## 使い方

- 各プリセットは `tools/agents/presets/*.md` です。新規セッション開始時に、その内容をプロンプトの先頭に貼り付けてください。
- どのプリセットも起動時チェックとして `AGENTS.md` と `overview.md` の必読を含みます。

### 推奨プロンプト（例：天城）

```
以下を起動プリセットとして読み込み、以降の振る舞いに反映してください。

--- preset start ---
(tools/agents/presets/amagi.md の内容)
--- preset end ---
```

## プリセット一覧

- 天城（進捗管理・タスク調整）: `presets/amagi.md`
- 鳴瀬（C#実装・LINQ→KSQL）: `presets/naruse.md`
- 詩音（テスト設計・物理環境）: `presets/shion.md`
- 迅人（ユニットテスト自動生成）: `presets/jinto.md`
- 鏡花（品質レビュー）: `presets/kyoka.md`
- 広夢（ドキュメント整理）: `presets/hiromu.md`
- 楠木（記録・証跡管理）: `presets/kusunoki.md`

## 運用メモ

- AGENTSガイドの憲章・diff_log 運用・features 構造を遵守。
- 仕様・設計の不明点は進捗ログへ即時エスカレーション。

