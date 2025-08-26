# chart UT確認観点

`docs/chart.md` の仕様から抽出したユニットテストで確認すべき観点。

## Tumbling
- minutes/hours/days/months の粒度指定ができ、`grace` で遅延許容を設定できる。`ensureContinuous: true` で欠損バケットを補完する。
- `GroupBy` の時間列は `Tumbling` に渡した列と同じで、バケット開始時刻に丸められる。

## TimeFrame
- `.TimeFrame<MarketSchedule>` は `.Tumbling` より先に呼び出し、結合条件で `Open <= Timestamp < Close` を明示する。

## 集約
- `GroupBy` では Broker, Symbol, BucketStart をキーにし、`Select` では `EarliestByOffset / LatestByOffset / Min / Max` だけで OHLC を算出する。

## 欠損補完・HB
- `ensureContinuous` と `WhenEmpty` により欠損バケットを前回値で補完し、HB が live/final の出力を駆動する。

## 遅延データ
- `grace` 以内の遅延は同じバケットを更新し、超過分は破棄される。

