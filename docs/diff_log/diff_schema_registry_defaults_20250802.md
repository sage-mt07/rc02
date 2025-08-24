# 差分履歴: schema_registry_defaults

🗕 2025-08-02
🧐 作業者: assistant

## 差分タイトル
スキーマレジストリURLのデフォルト化とAVROタイプ生成の競合回避

## 変更理由
テスト環境でスキーマレジストリURL未設定時に例外が発生し、並列実行で動的AVRO型が重複定義される問題を解消するため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `SpecificRecordGenerator` を `GetOrAdd` でスレッドセーフ化し、重複タイプ生成を防止
- Schema Registry に既定URLを設定し、無効な設定値による例外を回避
- Integrationテストプロジェクトに `coverlet.collector` を追加
- キー/値型名のサニタイズ仕様に合わせて `MappingRegistryTests` を更新

## 参考文書
- `docs_advanced_rules.md` B.1.2
