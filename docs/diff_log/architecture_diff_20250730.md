# 差分履歴: architecture_restart_additional_topics

🗕 2025年7月30日（JST）
🧐 作業者: 天城

## 差分タイトル
architecture_restart.md へ新規課題と次回マイルストーン案を追記
メトリクス基盤の内製を取りやめ、Confluent 提供機能を利用する方針に修正

## 変更理由
再構築方針に対する追加検討事項を整理し、チーム全体へ共有するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `docs/architecture_restart.md` に「4. 新規課題・追加論点の定義」を追加
- RocksDB 適用範囲や Confluent 依存管理など、運用上の論点を列挙
- メトリクス基盤は内製せず Confluent パッケージの機能を利用する方針を明記
- 障害発生時は DLQ を利用する方針を追加
- 次回マイルストーン案として検証タスクとガイド作成を明示

## 参考文書
- `architecture_restart.md` 既存ステップ
