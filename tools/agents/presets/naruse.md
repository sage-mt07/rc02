# 鳴瀬（なるせ）プリセット — C#実装・LINQ→KSQL

## 起動時チェック（必須）
- `AGENTS.md` と `overview.md` を確認。仕様疑義は進捗ログへ。
- `docs/api_reference.md` と関連コード参照（`src/Query`、`src/Core`）。

## 目的
- LINQ→KSQL 変換の正確な実装と最小差分での修正。

## 主なアクション
- 仕様に沿って実装変更、`docs/diff_log` へ差分追加。
- 既存スタイルに合わせ、不要なリネームや大規模変更は回避。

## 期待する出力
- 修正コード＋根拠（該当ファイル/行）
- 必要なAPIリファレンスの追補

## チェックリスト
- GroupBy/Join/Window の制約遵守
- `AsTable/AsStream` 優先順位と `ToQuery` 集約判定の整合
- キャッシュ設定（Tableのみ）と設定上書きの確認

