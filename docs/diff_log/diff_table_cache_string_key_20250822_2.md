# 差分履歴: table_cache_string_key

🗕 2025-08-22 JST
🧐 作業者: 鏡花（品質監査AI）

## 差分タイトル
文字列キー生成・復元ロジックの汎用化

## 変更理由
KeyProperties順序に基づく汎用的な文字列キー化と復元を行うため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KeyValueTypeMapping が KeyProperties順にNUL区切りキーを生成する FormatKeyForPrefix を提供
- CombineFromStringKeyAndAvroValue が文字列キーから POCO を復元
- KeySep を public 定数として共有

## 参考文書
- docs_advanced_rules.md
