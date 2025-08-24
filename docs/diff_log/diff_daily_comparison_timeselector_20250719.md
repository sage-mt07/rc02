# 差分履歴: daily_comparison_timeselector

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
WithWindow DSL に timeSelector 引数を追加

## 変更理由
ウィンドウをどの時刻プロパティで区切るかを明示的に指定できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `WithWindow<TEntity, TSchedule>` のパラメータに `timeSelector` を追加
- サンプル `KafkaKsqlContext.OnModelCreating` と README を更新
- API ドキュメントと移行ガイドに timeSelector の必須記述を追記

## 参考文書
- `docs/api_reference.md` bullet about timeSelector
