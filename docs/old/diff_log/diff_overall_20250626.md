# 差分レポート（全体監査）

🗕 2025年6月26日（JST）
🧐 作業者: 鏡花（品質監査AI）

## 指摘された横断的課題

- `KafkaContext` と `KsqlContext` 系のクラス名が混在しており、命名規則が統一されていない
- `ReadyStateMonitor` など StateStore/Monitoring 関連の実装が設計資料に明示されていない
- `CoreSettings.Validate` など未実装メソッドが存在し、Fail-Fast が徹底されていない
- EventSet 系のエラーハンドリング／DLQ 拡張が `oss_design_combined.md` に反映されていない

## 対応方針

- コンテキスト関連のクラス名をどちらかに統一し、設計書へ反映する
- StateStore や Monitoring の責務・設計を `oss_design_combined.md` に追記
- 未実装メソッドを削除または実装し、テストにて Fail-Fast 動作を検証
- エラーハンドリング・DLQ 処理の仕様をドキュメントへ記述

## 該当設計資料

- `oss_design_combined.md` セクション 3.4
- `docs_advanced_rules.md` セクション 2

## 関連diffリンク

(なし)
