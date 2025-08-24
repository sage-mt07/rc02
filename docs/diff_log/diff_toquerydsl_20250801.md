# 差分履歴: toquerydsl_initial

🗕 2025年8月1日（JST）
🧐 作業者: codex

## 差分タイトル
ToQuery DSL 用のクエリモデル実装

## 変更理由
KSQL 生成を支える DSL の基盤として、From〜Select の構造を保持するモデルが必要になったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KsqlQueryModel` クラスを追加し、各種式を保持
- `WindowDefinition` クラスを定義し、TUMBLING ウィンドウ情報を扱えるようにした
- `KsqlQueryable2` クラスで `.Where()`, `.Select()`, `.Tumbling()` を連鎖保持
- `Kafka.Ksql.Linq.csproj` に `Query/Dsl` フォルダーを追加

## 参考文書
- `docs/namespaces`

