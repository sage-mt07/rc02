# 差分履歴: fluentapi_samples

🗕 2025年7月24日（JST）
🧐 作業者: naruse

## 差分タイトル
サンプルコードをtests配下へ移動しビルドエラーを修正

## 変更理由
examplesディレクトリでは参照しづらかったため、テスト用サンプルとしてtests内に配置し直した。

## 追加・修正内容（反映先: oss_design_combined.md）
- `FluentApiSamples` ディレクトリを tests プロジェクトに追加
- `SampleContext` で `using System` を追加
- `FluentSampleIntegrationTests` で Order 型のエイリアス指定

## 参考文書
- `docs/fluent_api_initial_design.md`
