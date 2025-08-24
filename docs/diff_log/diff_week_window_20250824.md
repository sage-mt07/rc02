# 差分履歴: week_window

🗕 2025-08-24 13:32 JST
🧐 作業者: 鳴瀬

## 週足ウィンドウ追加
- Tumbling DSL に week パラメータを追加し 1wk ウィンドウを宣言可能に
- Period に Week(anchor) を追加し週境界の丸め/加算を実装
- Topic 名・TimeBucket が 1wk を解決

## 変更理由
- 週次集計ロールアップ対応のため

## 追加・修正内容（反映先: oss_design_combined.md）
- 1wk を Windows/Period に追加
- 1wk は 1m_final からロールアップ

## 参考文書
- docs_advanced_rules.md セクション B.1.2
