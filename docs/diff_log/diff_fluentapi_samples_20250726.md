# 差分履歴: fluentapi_samples

🗕 2025年7月26日（JST）
🧐 作業者: naruse

## 差分タイトル
サンプルを src/Entities/Samples へ移動し DI 登録を即時化

## 変更理由
テストプロジェクトから独立したサンプル配置とし、`AddSampleModels` 呼び出し後に即モデルが利用できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- サンプルクラスを tests から src/Entities/Samples へ移動
- `AddSampleModels` で ModelBuilder と MappingManager を即生成し登録

## 参考文書
- `docs/fluent_api_initial_design.md`
