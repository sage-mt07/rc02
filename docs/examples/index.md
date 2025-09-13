# Examples Index

このページは、すぐに試せる最小サンプルへの導線です。環境準備のうえ、該当ドキュメントのコードを貼って動作を確認してください。

## Basics
- 単純フィルタ＋投影: `docs/onmodelcreating_samples.md#1-単純フィルタ＋投影（pullpushどちらでも）`
- DECIMAL 精度の固定: `docs/onmodelcreating_samples.md#6-decimal-精度の固定（属性）`

## Aggregations
- GroupBy＋集計（Push）: `docs/onmodelcreating_samples.md#3-groupby＋集計（push配信）`
- HAVING 句: `docs/onmodelcreating_samples.md#4-having-句で閾値を絞る`

## Joins
- 2ストリームJOIN（WITHIN 必須）: `docs/onmodelcreating_samples.md#2-2ストリームjoin（within-必須）`

## Windows
- TUMBLING 1分: `docs/onmodelcreating_samples.md#7-時間窓（tumbling-1分push）`

## Error handling / DLQ（計画）
- マッピング例外→DLQ（テスト実装参照、サンプルは後日追加）

## 共通チェックリスト
- JOIN に `.Within(...)` がある
- 集計と非集計の混在は GROUP BY で解消
- DECIMAL は `[KsqlDecimal(p,s)]` で精度固定
- Push/Pull は自動推論（GroupBy 等で Push、TABLE は Pull）

## 関連リファレンス
- 関数/型対応表: `docs/ksql-function-type-mapping.md`
- SQLServer→ksqlDB ガイド: `docs/sqlserver-to-kafka-guide.md`
