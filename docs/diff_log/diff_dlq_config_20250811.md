# 差分履歴: DLQ設定拡張

🗕 2025-08-11 (JST)
🧐 作業者: 鳴瀬

## 設定外出しと制御の導入
- DlqOptionsを拡張し機能フラグ・サンプリング・レート制限を追加
- KafkaConsumerManagerとEventSetでShouldSendガードを使用
- DlqEnvelopeFactoryでスタックトレース正規化をオプション化

## 変更理由
- リリース後の動的調整と運用ポリシー適用を容易にするため

## 追加・修正内容（反映先: oss_design_combined.md）
- DlqOptionsにヘッダ許可リストや例外除外設定を追加
- DLQ送信前のサンプリングとレート制御処理

## 参考文書
- docs/trace/appsettings_to_namespaces.md
