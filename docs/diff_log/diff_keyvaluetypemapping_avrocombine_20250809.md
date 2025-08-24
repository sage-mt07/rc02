# 差分履歴: keyvaluetypemapping_avrocombine

🗕 2025-08-09
🧐 作業者: 鳴瀬

## 高速Avro合成の導入
- Avro ISpecificRecord → POCO 変換を式木デリゲートで最適化
- Nullable, alias, timestamp-millis 等の変換を ConvertIfNeeded に集約
- キャッシュキー: (POCO型, Avro型, スキーマ指紋)

## 変更理由
- 反射ループによるオーバーヘッドを排除し、スキーマ進化に耐えるため

## 追加・修正内容（反映先: oss_design_combined.md）
- KeyValueTypeMapping.CombineFromAvroKeyValue をキャッシュ付き式木実装に更新
- PropertyMeta に SourceName を追加し Avro 名マッピングを拡張

## 参考文書
- docs/architecture/key_value_flow.md
