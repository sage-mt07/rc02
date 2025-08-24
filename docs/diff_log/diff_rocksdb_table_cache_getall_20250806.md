# 差分履歴: RocksDbTableCache GetAll

🗕 2025-08-06 (JST)
🧐 作業者: assistant

## 差分タイトル
RocksDbTableCache.GetAll で POCO のみを返すよう変更

## 変更理由
既存の `GetAll` メソッドはキーと値のペアを返していたが、利用側では値のみを使用しており冗長だったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `ITableCache<T>.GetAll` の戻り値を `IEnumerable<T>` に変更
- `RocksDbTableCache<T>.GetAll` が POCO を列挙するよう修正
- 呼び出し側 (`WindowFinalConsumer`, `ReadCachedEntitySet`) を新しい戻り値に対応

## 参考文書
- `docs/namespaces/cache_namespace_doc.md` セクション "主要コンポーネント責務"
