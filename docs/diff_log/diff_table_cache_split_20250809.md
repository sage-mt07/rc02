# 差分履歴: table_cache_split

🗕 2025-08-09
🧐 作業者: 鳴瀬

## 差分タイトル
POCO単位のKafkaStreamとTableCache初期化

## 変更理由
型閉じ済みStoreを一度だけ取得して高速な全件取得を可能にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- TableCache と TableCacheRegistry を再実装し、シンプルなキャッシュ管理に変更
- UseTableCache で POCO ごとに StreamBuilder と KafkaStream を構築
- ToListAsync で CombineFromKeyValue 経由の列挙を実装
- KafkaStream の初期状態取得と Store 名解析からリフレクションを排除
- IKafkaStreams.WaitUntilRunningAsync を導入し、イベントベースで RUNNING を待機

## 参考文書
- docs/namespaces/cache_namespace_doc.md
