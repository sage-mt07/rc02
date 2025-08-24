# 差分履歴: fluentapi_samples

🗕 2025年7月25日（JST）
🧐 作業者: naruse

## 差分タイトル
csproj の Compile 重複設定を削除

## 変更理由
サンプルソースを tests 配下へ移動した際、既定の Compile と重複する項目を追加してしまいビルドエラーとなったため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `Kafka.Ksql.Linq.Tests.csproj` から `FluentApiSamples` 関連の `<Compile>` 行を削除

## 参考文書
- `docs/fluent_api_initial_design.md`

