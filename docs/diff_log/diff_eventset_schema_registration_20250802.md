# 差分履歴: eventset_schema_registration

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
EventSet<T> POCO registration after OnModelCreating

## 変更理由
OnModelCreating はクエリ定義専用であり、クエリを伴わない `EventSet<T>` の POCO をその完了後に登録する必要があったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KsqlContext コンストラクタで `ConfigureModel()` 実行後に `EventSet<T>` プロパティを初期化し、スキーマ登録を実施。
- `CreateEntityModelFromType` が `[KsqlTopic]` と `[KsqlKey]` 属性を評価してトピック名とキー順序を設定。
- ドキュメントを更新し、OnModelCreating と `EventSet<T>` 登録の役割を明確化。

## 参考文書
- `docs/getting-started.md` セクション 4
