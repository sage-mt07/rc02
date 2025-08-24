# 差分履歴: rocksdb_table_cache

🗕 2025-08-05 (JST)
🧐 作業者: naruse

## 差分タイトル
RocksDbTableCache.InitializeAsyncでStoreQueryParametersの型推論を利用

## 変更理由
StoreQueryParameters.FromNameAndTypeのジェネリック指定が過剰で、APIの想定と異なっていたため。

## 追加・修正内容（反映先: oss_design_combined.md）
- FromNameAndType呼び出しから明示的なジェネリック型引数を除去し、型推論に任せることでStreamizの利用例に合わせた。

## 参考文書
- なし
