# 差分履歴: schema_registration

🗕 2025年8月2日（JST）
🧐 作業者: assistant

## 差分タイトル
事前にAvroスキーマを登録し、DDLのWITH句でschema IDを指定

## 変更理由
ksqlDBのCREATE文とSchema Registryのスキーマを紐付け、互換性を担保するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- エンティティごとにキー/値スキーマを生成・登録し、schema IDをEntityModelへ保存
- CREATE STREAM/TABLEおよびCREATE ... AS文でKEY_SCHEMA_ID/VALUE_SCHEMA_IDをWITH句に付与
- Schema登録失敗時にロギングし処理を中断

## 参考文書
- `docs_advanced_rules.md` セクション 2
