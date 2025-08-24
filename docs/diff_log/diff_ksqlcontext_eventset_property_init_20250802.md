# 差分履歴: ksqlcontext_eventset_property_init

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
KsqlContext constructor instantiates EventSet properties

## 変更理由
コンテキストクラスの EventSet<T> プロパティが null のまま使用されることを防止し、明示的な初期化を不要にするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- KsqlContext コンストラクタで public EventSet<T> プロパティをリフレクションで検出し、自動的に `CreateEntitySet` で生成。
- 生成したセットを `_entitySets` に登録し、再利用可能にした。

## 参考文書
- `docs_advanced_rules.md` セクション B.1.2
