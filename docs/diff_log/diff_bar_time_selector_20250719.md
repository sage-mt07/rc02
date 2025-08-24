# 差分履歴: bar_time_selector

🗕 2025-07-19 (JST)
🧐 作業者: assistant

## 差分タイトル
BarTime セレクター自動抽出と Limit API 更新

## 変更理由
Bar エンティティの時刻プロパティ名を固定せず、`Select<RateCandle>` で指定した割り当てから自動取得することで拡張性を高めるため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `EntityModel` に `BarTimeSelector` プロパティを追加
- `WindowSelectionBuilder.Select` で `BarTime` への代入を解析しセレクターを保存
- `EventSetLimitExtensions.Limit` で `BarTimeSelector` を使用して並び替え・削除を実行
- テスト `EventSetLimitExtensionsTests` を更新

## 参考文書
- `docs/api_reference.md` Window セクション
