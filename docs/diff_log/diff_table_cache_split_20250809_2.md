# 差分履歴: table_cache_split

🗕 2025-08-09
🧐 作業者: 鳴瀬

## 差分タイトル
ストア単位のRUNNING待機に変更

## 変更理由
POCO単位で起動したKafkaStreamごとに状態を監視するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- IKafkaStreams.WaitUntilRunningAsync をストア名指定に変更
- MultiStreamizKafkaStreams がストアごとに TaskCompletionSource を管理
- TableCache がストア名を保持して WaitUntilRunningAsync を呼び出す

## 参考文書
- docs/namespaces/cache_namespace_doc.md
