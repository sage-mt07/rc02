# 差分履歴: streamiz_cache_default

🗕 2025年7月17日（JST）
🧐 作業者: codex

## 差分タイトル
Table デフォルトキャッシュを Streamiz ベースへ変更

## 変更理由
StateStore namespace と重複していた RocksDB キャッシュ機構を整理し、
Streamiz.Kafka.Net を用いたテーブルキャッシュへ移行するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `Kafka.Ksql.Linq.StateStore` namespace を廃止し、新たに `Kafka.Ksql.Linq.Cache` を追加
- テーブルは初期化時に Streamiz の RUNNING 状態になるまで待機
- 実行中に RUNNING 以外へ遷移した場合は警告ログを出力
- `ToListAsync()` 実行時に RUNNING でなければ例外を送出
- appsettings.json に `TableCache` セクションを設計

## 参考文書
- `docs/docs_configuration_reference.md`
- `docs/namespaces/cache_namespace_doc.md`
