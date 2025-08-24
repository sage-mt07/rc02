# 差分履歴: groupby_key_order

🗕 2025年7月9日（JST）
🧐 作業者: 広夢（戦略広報AI）

## 差分タイトル
GroupBy/Joinキー順チェック仕様の追加

## 変更理由
LINQ式で生成される論理キー順とDTO定義順の不一致を防ぎ、設計・ビルド・テスト段階で即時検出できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `poco_design_policy.md` に検証ロジックとエラーメッセージを追記
- `docs_advanced_rules.md` に ValidationMode: Strict での同順チェック説明を追加

## 参考文書
- `docs_advanced_rules.md` セクション 7
