# 差分履歴: daily_comparison_context

🗕 2025年7月19日（JST）
🧐 作業者: codex

## 差分タイトル
DailyComparison サンプルのバー定義を OnModelCreating へ統一

## 変更理由
Aggregator クラスで行っていたバー/ウィンドウ集約の定義を削除し、
KafkaKsqlContext の DSL 定義に移管する方針に合わせるため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `KafkaKsqlContext.OnModelCreating` で `WithWindow<Rate, MarketSchedule>` を使用して 1/5/60 分足を宣言
- Aggregator は定義済みの `RateCandle` と `DailyComparison` セットを参照するだけに変更
- README に DSL 記述例を追記

## 参考文書
- `docs/api_reference.md` "バーやウィンドウ定義は ... OnModelCreating" 箇所
