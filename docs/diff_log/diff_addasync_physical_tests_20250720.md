# 差分履歴: addasync_physical_tests

🗕 2025年7月20日（JST）
🧐 作業者: assistant

## 差分タイトル
PhysicalTests を AddAsync 拡張版へ移行

## 変更理由
- 既存テストが `KafkaProducerManager` 経由で `SendAsync` を呼び出していた
- AddAsync 標準化方針に合わせ、テスト側でも AddAsync を使用する形に統一

## 追加・修正内容（反映先: oss_design_combined.md）
- `DummyFlagMessageTests` `DummyFlagSchemaRecognitionTests` `SchemaNameCaseSensitivityTests` を AddAsync 使用に改修
- `EventSetExtensions` を追加し `KafkaMessageContext` を渡せる拡張メソッドを実装

## 参考文書
- `diff_addasync_standardization_20250727.md`
