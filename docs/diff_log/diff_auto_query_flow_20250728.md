# 差分履歴: auto_query_flow

🗕 2025年7月28日（JST）
🧐 作業者: 鏡花

## 差分タイトル
AddAsync公式化と自動フロー例追加

## 変更理由
AddAsyncを推奨APIとして明確化し、QueryからKsqlContextまで自動的に連携する流れを示すため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `query_to_addasync_sample.md` を自動フロー版に更新
- `FullAutoQueryFlowTests` を追加し DI での Query→MappingManager→KsqlContext 経路を検証
- 進捗ログ 20250728 を追加
- サンプルとテストをQueryable式利用に修正

## 参考文書
- `docs_advanced_rules.md` サンプル整合性セクション
