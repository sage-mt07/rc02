# 差分履歴: DLQオプション移行

🗕 2025-08-10 (JST)
🧐 作業者: 鳴瀬

## DlqTopicConfiguration廃止
DLQ設定を DlqOptions へ統合。

## 変更理由
- 設定項目の重複を排除し、構成を簡素化するため

## 追加・修正内容（反映先: oss_design_combined.md）
- DlqOptionsへ保持期間・パーティション等の設定を追加
- KsqlDslOptions から DlqTopicConfiguration を削除
- 関連ドキュメントとサンプル設定を更新

## 参考文書
- docs/trace/appsettings_to_namespaces.md

