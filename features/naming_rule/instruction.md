# naming_rule / instruction

## 目的・背景
- ClassName を snake_case に変換し、辞書KVで物理トピック名を解決する。

## スコープ
- 含む: snake_case 変換、辞書KVクライアント、KSQL DDL への物理名適用
- 含まない: pub/int SerDe 管理

## 成果物・完了条件
- [x] コード: 命名変換と辞書解決ロジック
- [x] テスト: SnakeCaseConverter と PhysicalTopicNameResolver
- [x] diff_log: `docs/diff_log/diff_naming_rule_20250831.md`
