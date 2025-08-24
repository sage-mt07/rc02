# architecture_restart 指示一覧
作成日: 2025-07-13 (JST)
作成者: 広夢・楠木

## 各担当へのタスク
- **鳴瀬**
  - `src/Core/MappingManager.cs` と `features/mapping_manager/instruction.md` を突き合わせ、複合キー対応・型変換ロジック・例外処理を拡充
  - AddAsync 標準化サンプルを `examples/naruse/mapping_manager/` へ追加し、テストは `tests/Mapping/` 配下に配置
  - 進捗と疑問点は `docs/changes/` の当日ログへ追記し、完了時に `docs/diff_log/` へ差分記録。相談事項は PM 天城へ即共有
- **詩音 ＆ 鏡花**
  - 上記実装をレビューし、API 仕様と例外設計のチェック結果を `features/mapping_manager/viewpoints.md` に記載
  - 複合キー・型変換を含むテスト観点をまとめ、実行結果を `docs/diff_log/` に保存
- **迅人**
  - `tests/FullAutoQueryFlowTests.cs` を拡充して自動フローを検証し、`dotnet test` の結果を `docs/changes/` に記録
- **広夢 ＆ 楠木**
  - `docs/architecture` 配下のストーリーと設計ガイドを更新し、指示一覧を `Reportsx/kusunoki/` へ整理
  - ドキュメント更新内容を `docs/diff_log/` にも記録し、疑問点は天城へ報告
- **天城（PM）**
  - Step3 の残課題を整理して各担当へ割り振り、Step4 の検証タスクと次回マイルストーンを `docs/changes/` に追記
