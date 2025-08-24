# 差分履歴: table_cache_string_key

🗕 2025-08-22 JST
🧐 作業者: 鏡花（品質監査AI）

## 差分タイトル
TableCache にテスト用フックを追加し、NUL 区切りフィルタの UT を整備

## 変更理由
前方一致フィルタの挙動を Kafka 依存なしで検証するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- TableCache に internal コンストラクタとテスト用委譲を追加
- NUL 区切りフィルタと復元の単体テストを追加
- Kafka.Ksql.Linq に InternalsVisibleTo を追加

## 参考文書
- docs_advanced_rules.md
