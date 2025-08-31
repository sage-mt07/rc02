# diff_features_scaffold_20250831

## 概要
- `features/` ワークスペースを新設し、テンプレートを配置
- `overview.md` に features 運用の参照行を追記

## 変更点
- 追加: `features/README.md`
- 追加: `features/_template/instruction.md`
- 変更: `overview.md` に以下を追記
  - 機能別作業の集約は `features/` を参照（テンプレート: `features/_template/`）

## 背景・目的
- AGENTSガイドの「featuresディレクトリの活用」を具体化し、機能単位の作業起点を統一
- 設計差分（docs/diff_log）と日次進捗（docs/changes）との連携を円滑化

## 影響範囲
- ドキュメントのみ（コードロジックの変更なし）
- 新規機能着手時は `features/_template/instruction.md` をコピーして開始可能

## フォローアップ
- 次の新規機能/マイグレーションから本構造を適用
- 既存の大きな作業単位がある場合は段階的に features へ移管

