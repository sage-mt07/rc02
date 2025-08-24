# 差分履歴: eventset_constructor_init

🗕 2025-08-02 (JST)
🧐 作業者: assistant

## 差分タイトル
EventSet constructor-based entity registration

## 変更理由
EventSet プロパティをコンストラクタで初期化する設計に合わせ、インスタンス化時に POCO 型情報を自動登録するため。

## 追加・正内容（反映先: oss_design_combined.md）
- KsqlContext に EnsureEntityModel を追加し、EventSet 生成時にエンティティ登録とDDL生成を自動実行。
- EventSet コンストラクタから EnsureEntityModel を呼び出し、プロパティ初期化だけで登録が完了するよう改修。

## 参考文書
- `docs_advanced_rules.md` セクション B.1.2
