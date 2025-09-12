# Advanced Rules and Patterns (Bars, Schedules, Weekly Handling)

本ドキュメントは、イベント時刻ベースの足生成、MarketSchedule 依存の集計、週次の取り扱い、遅延レコード（grace）などの実運用に必要な要点を、利用者向けに分かりやすくまとめたものです。DSL と ksqlDB の性質差を踏まえ、誤解の多いポイントを先に解説します。

## 1. イベント時刻（Event Time）が基準
- 足（Tumbling Window）の丸め・集計は、Kafka レコードの「イベント時刻」を基準に処理します。
- テストやバッチでは `Rate.Timestamp`（例）に明示的に日時を入れれば、実時間を待たずに未来・過去の任意ウィンドウを即時に作成可能です。
- 運用上の待機は、サービス初期化・DDL安定・クエリ応答のポーリング程度で、ウィンドウ時間長への依存はありません。

## 2. ウィンドウと EMIT（CHANGES / FINAL）
- 集計は Tumbling Window を利用します（例: 1m/5m/15m/60m、あるいは 1d/7d）。
- ライブ系（Live）は `EMIT CHANGES`、確定系（Final）は `EMIT FINAL` を使い分けます。
- Close は `LatestByOffset(...)`（最終オフセット）で定義し、Open/High/Low と組み合わせて OHLC を構成します。

## 3. 週の概念（Week Anchor）と ksqlDB の性質
- DSL 側には WeekAnchor の概念があり、既定は「Monday（週の開始は月曜）」です。
- 一方、ksqlDB の Window 自体には“曜日アンカー”の概念がありません。`SIZE 7 DAYS` はエポックから等間隔で切られます。
- 実務では、**MarketSchedule（営業日カレンダー）**により「どの日を MarketDate（集計の代表日）にするか」を決め、**TimeFrame** で結合・日単位のキー付け（`dayKey`）を行うことで、週起点（月曜）や休業日の扱いを **論理的に保証** します。

### 週次（7日）を“月曜起点”で扱う推奨パターン
1. MarketSchedule に `Broker, Symbol, MarketDate(=その営業日), Open, Close`（土日は定義しない）を投入。
2. DSL で `TimeFrame<MarketSchedule>` を用い、`s.Open <= r.Timestamp && r.Timestamp < s.Close` を満たすレコードだけを営業日に紐付ける。
3. `dayKey: s => s.MarketDate` を指定し、日/週のキーを MarketSchedule ベースで安定させる。
4. 週次は Tumbling Days=7 で集計（MarketDate が平日のみ供給される前提で週単位が安定）。

## 4. MarketSchedule のモデル化と利用
- Topic: 例 `marketschedule`
- 推奨フィールド:
  - `Broker, Symbol`（キー）
  - `Open, Close`（営業開始・終了時刻）
  - `MarketDate`（営業日の代表日。週起点・祝日ロジックもここで表現）
- DSL 利用例（疑似コード）:

```csharp
modelBuilder.Entity<Bar>()
  .ToQuery(q => q.From<Rate>()
    .TimeFrame<MarketSchedule>((r, s) =>
         r.Broker == s.Broker
      && r.Symbol == s.Symbol
      && s.Open <= r.Timestamp && r.Timestamp < s.Close,
      dayKey: s => s.MarketDate)
    .Tumbling(r => r.Timestamp, new Windows { Days = new[] { 1, 7 } })
    .GroupBy(r => new { r.Broker, r.Symbol })
    .Select(g => new Bar
    {
        Broker = g.Key.Broker,
        Symbol = g.Key.Symbol,
        BucketStart = g.WindowStart(),
        Open  = g.EarliestByOffset(x => x.Bid),
        High  = g.Max(x => x.Bid),
        Low   = g.Min(x => x.Bid),
        KsqlTimeFrameClose = g.LatestByOffset(x => x.Bid)
    }));
```

## 5. 休業日の扱い（例: 土日休み）
- MarketSchedule に **営業日だけ** `Open/Close/MarketDate` を供給し、土日は供給しないことで、日次バーは該当しない日付では生成されません。
- 週次は平日のみが集計対象になるため、土日を含む 7 日タムリングでも “実質的に” 平日だけが計算されます。
- 物理検証の例:
  - 直近の週の月〜金にスケジュール行を投入（土日なし）
  - `Rate` は月〜日の正午に1本ずつ投入
  - `bar_1d_live` に平日5本のみ出現、`bar_1wk_final` に週1本出現

## 6. 遅延レコード（grace）と境界近傍
- `grace`（許容遅延）を設けると、**境界後に到着したイベント**でもイベント時刻がウィンドウ内であれば、そのウィンドウに吸収され、High/Low/Close が更新されます。
- テストでは、境界直前/直後に極値レコードを投入して、隣接ウィンドウへの誤配がないことを検証しています。

## 7. マルチティア（1m/5m/15m/60m/1d/1wk）の作り方
- `new Windows { Minutes = new[] { 1, 5, 15, 60 } }` や `Days = new[] { 1, 7 }` のように複数フレームを同時に指定できます。
- DSL → QueryModel → DDL では、各ティアの CSAS/CTAS が派生作成され、`bar_1m_live`, `bar_5m_live`, `bar_15m_live`, `bar_60m_live`, `bar_1d_live`, `bar_1wk_final` などが生成されます。

## 8. Push / Pull の使い分け（HTTP直叩きの推奨形）
- Push: `SELECT ... EMIT CHANGES LIMIT N`（/query-stream）で「生成の疎通」を待機（取りこぼし防止）。
- Pull: `SELECT ... FROM <table>`（/query）で確定状態を取得・検証。
- 実装上の差異に備え、/query は `{"sql":"...","properties":{}}` を基本とし、必要時に `ksql` フィールドや push へのフォールバックを用意すると堅牢です。

## 9. 運用ヒント（パフォーマンス/安定性）
- ログ: `KSQL_LOG4J_ROOT_LOGLEVEL=INFO`（DEBUGは抑制）
- GC: `-XX:+UseG1GC -XX:MaxGCPauseMillis=100`（短ポーズ優先）
- クエリ: `KSQL_KSQL_QUERY_TIMEOUT_MS=300000`（5分）や `KSQL_KSQL_QUERY_PULL_MAX_ALLOWED_OFFSET_LAG` の調整
- Kafka の内外リスナー: `PLAINTEXT://localhost:9092, INTERNAL://kafka:29092` の二系統を正しく設定（ksqlDB/Schema Registry は `kafka:29092` を参照）

## 10. 代表パターン（まとめ）
- 週の概念: DSLの WeekAnchor は Monday（既定）。ksqlDBの Window に曜日アンカーはないため、MarketSchedule の `MarketDate` で論理的に固定。
- 日次/週次: `TimeFrame + Tumbling(Days={1|7})`、`g.WindowStart()` を BucketStart とし、OHLC は Earliest/Max/Min/Latest を利用。
- 休業日: MarketSchedule に営業日だけ供給（土日なし）→ 日次バーは平日のみ生成。
- 遅延・境界: grace でイベント時刻ベースの吸収を許容（境界直前/直後の極値で検証）。

---
本書の内容は `physicalTests` 下の各テスト（ロングラン、多段、スケジュール依存）で物理検証済みです。利用者が独自のスケジュールや祝日カレンダーを扱う際は、MarketSchedule の `MarketDate/Open/Close` を適切に設計し、上記のパターンをベースに DSL を組み立ててください。

## 付録: marketschedule を週で運用するためのデータ例

以下は「週を月曜起点」「土日休み」で運用する最小例です。Broker/ Symbol は固定（B1/S1）、UTC で 09:00–15:00 の営業。土日分の行は投入しません。

### 1) 概念レコード（月〜金のみ）
| Broker | Symbol | MarketDate (UTC) | Open (UTC)        | Close (UTC)       |
|--------|--------|------------------|-------------------|-------------------|
| B1     | S1     | 2025-09-08       | 2025-09-08 09:00  | 2025-09-08 15:00  |
| B1     | S1     | 2025-09-09       | 2025-09-09 09:00  | 2025-09-09 15:00  |
| B1     | S1     | 2025-09-10       | 2025-09-10 09:00  | 2025-09-10 15:00  |
| B1     | S1     | 2025-09-11       | 2025-09-11 09:00  | 2025-09-11 15:00  |
| B1     | S1     | 2025-09-12       | 2025-09-12 09:00  | 2025-09-12 15:00  |

> 注意: 2025-09-13（土）、2025-09-14（日）は投入しない（休業日）。

### 2) ksqlDB からの投入（Pull/Push検証用）
DDL（すでに自動生成済みであることが多い）:

```sql
CREATE STREAM IF NOT EXISTS MARKETSCHEDULE (
  BROKER STRING KEY,
  SYMBOL STRING KEY,
  OPEN   TIMESTAMP,
  CLOSE  TIMESTAMP,
  MARKETDATE TIMESTAMP
) WITH (
  KAFKA_TOPIC='marketschedule',
  KEY_FORMAT='AVRO', VALUE_FORMAT='AVRO'
);
```

INSERT 例（UTC、月〜金の5営業日）:

```sql
INSERT INTO MARKETSCHEDULE (BROKER, SYMBOL, OPEN, CLOSE, MARKETDATE) VALUES
  ('B1','S1', TIMESTAMP '2025-09-08 09:00:00', TIMESTAMP '2025-09-08 15:00:00', TIMESTAMP '2025-09-08 00:00:00');
INSERT INTO MARKETSCHEDULE (BROKER, SYMBOL, OPEN, CLOSE, MARKETDATE) VALUES
  ('B1','S1', TIMESTAMP '2025-09-09 09:00:00', TIMESTAMP '2025-09-09 15:00:00', TIMESTAMP '2025-09-09 00:00:00');
INSERT INTO MARKETSCHEDULE (BROKER, SYMBOL, OPEN, CLOSE, MARKETDATE) VALUES
  ('B1','S1', TIMESTAMP '2025-09-10 09:00:00', TIMESTAMP '2025-09-10 15:00:00', TIMESTAMP '2025-09-10 00:00:00');
INSERT INTO MARKETSCHEDULE (BROKER, SYMBOL, OPEN, CLOSE, MARKETDATE) VALUES
  ('B1','S1', TIMESTAMP '2025-09-11 09:00:00', TIMESTAMP '2025-09-11 15:00:00', TIMESTAMP '2025-09-11 00:00:00');
INSERT INTO MARKETSCHEDULE (BROKER, SYMBOL, OPEN, CLOSE, MARKETDATE) VALUES
  ('B1','S1', TIMESTAMP '2025-09-12 09:00:00', TIMESTAMP '2025-09-12 15:00:00', TIMESTAMP '2025-09-12 00:00:00');
```

### 3) .NET AddAsync での投入（Integration テストの例）
```csharp
// 月曜を基準に当週の平日5日分を投入
var monday = DateTime.UtcNow.Date;
int delta = ((int)DayOfWeek.Monday - (int)monday.DayOfWeek + 7) % 7;
monday = monday.AddDays(delta);
for (int i = 0; i < 7; i++)
{
  var d = monday.AddDays(i);
  if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
  await ctx.Schedules.AddAsync(new MarketSchedule
  {
    Broker = "B1",
    Symbol = "S1",
    MarketDate = d,
    Open  = d.AddHours(9),
    Close = d.AddHours(15)
  });
}
```

### 4) Rate 側の投入（サンプル）
```csharp
// 月〜日の正午に1本ずつ（休業日も投入可。TimeFrameで除外される）
for (int i = 0; i < 7; i++)
{
  var d = monday.AddDays(i);
  await ctx.Rates.AddAsync(new Rate { Broker = "B1", Symbol = "S1", Timestamp = d.AddHours(12), Bid = 100 });
}
```

### 5) 期待される挙動
- 日次（bar_1d_live）: 平日 5 本のみ生成（休業日は生成されない）。
- 週次（bar_1wk_final）: 週 1 本生成（平日のみが集計対象）。
- 週の起点（Monday）は MarketDate 設計で担保されます（ksqlDB の `SIZE 7 DAYS` 自体には曜日アンカーはありません）。

