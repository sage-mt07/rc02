# 差分履歴: foreachasync_meta

🗕 2025-08-11 JST
🧐 作業者: 鳴瀬

## 差分タイトル
ForEachAsync API unified with MessageMeta

## 変更理由
旧 (entity, headers) インターフェースを段階的に廃止し、メッセージメタデータを全処理系で扱うため。

## 追加・修正内容（反映先: oss_design_combined.md）
- ForEachAsync(Func<T, Dictionary<string,string>, MessageMeta, Task>) を新設
- 旧 ForEachAsync(Func<T, Dictionary<string,string>, Task>) を [Obsolete] 化
- KafkaConsumerManager.ConsumeAsync を MessageMeta 対応に統一
- ドキュメント、サンプルを新シグネチャへ更新

## 参考文書
- docs/getting-started.md
- README.md
