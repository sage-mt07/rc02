# 差分履歴: TumblingAnalyzer 削除

🗕 2025-09-07 JST
🧐 作業者: 鳴瀬

## TumblingAnalyzer を削除
旧実装の解析クラス `TumblingAnalyzer` を削除しました。

## 変更理由
- 解析処理は他コンポーネントに移行し、未使用となっていたため。

## 影響範囲
- `src/Query/Analysis/TumblingAnalyzer.cs` を削除
- 過去の差分ログ `diff_timeframe_daykey_20250824.md` の該当箇所を整理

