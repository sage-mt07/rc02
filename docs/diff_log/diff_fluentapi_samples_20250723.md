# 差分履歴: fluentapi_samples

🗕 2025年7月23日（JST）
🧐 作業者: naruse

## 差分タイトル
Fluent API サンプル実装と MappingManager 連携テスト

## 変更理由
初期デザインを具体化し、代表的な POCO を Fluent API で登録する手順を示すため

## 追加・修正内容（反映先: oss_design_combined.md）
 - `examples/fluent-api-sample` に User, Product, Order の POCO と登録拡張メソッドを追加
- `tests/Mapping/FluentSampleIntegrationTests.cs` を新規追加
- DI 用の `AddSampleModels` 拡張を示し、MappingManager 登録例を実装

## 参考文書
- `docs/fluent_api_initial_design.md`
