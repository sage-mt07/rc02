# 差分履歴: composite_key_tuple_conversion

🗕 2025年7月13日（JST）
🧐 作業者: assistant

## 差分タイトル
複合キー抽出のTuple方式導入

## 変更理由
Dictionary形式では型情報が失われるため、送信時の安全な型変換を保証する目的。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KeyExtractor` に `ExtractKeyParts` と `BuildTypedKey` を追加
- `MappingManager` と `KafkaProducer` を新方式で実装
- ドキュメントサンプルを新APIに更新

## 参考文書
- `docs/architecture/key_value_flow.md`
