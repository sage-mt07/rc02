# 差分履歴: daily_comparison_extensionmove

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
ModelBuilderWindowExtensions を src へ移動

## 変更理由
ウィンドウ定義 DSL をサンプルではなく本体ライブラリに配置する方針とするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- ModelBuilderWindowExtensions と関連クラスを src 配下に新設
- KafkaKsqlContext から新しい名前空間を参照するよう更新

## 参考文書
- `docs/api_reference.md` バー定義セクション
