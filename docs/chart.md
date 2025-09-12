# 足生成 DSL ガイド（日本語整理版）

このドキュメントは「何ができるか」→「どう動くか」→「何に注意するか」の順で、足生成 DSL の全体像をわかりやすく説明します。

できること
- Tick（レートやトレード）から、秒/分/時間/日/週/月の足を生成できる
- 1 つのクエリで複数のタイムフレーム（例: 1m/5m/1h/1d）をまとめて宣言できる
- MarketSchedule（営業日カレンダー）で日/週の境界を安定させられる
- Table は RocksDB にマテリアライズされ、`ToListAsync()` で素早く参照できる

---

## 1. 全体像（まずここだけ読む）

処理フロー（上から下へ）
- 入力: 取引時間外を除いたストリーム（例: `<raw>_filtered`）
- スケジュール結合: `TimeFrame<MarketSchedule>(…, dayKey: …)` で「取引時間内だけ」を選び、日/週の境界を固定
- 窓生成: `Tumbling(r => r.Timestamp, Windows{…}, grace: …)` で複数足を一括生成
- 集計: `GroupBy(...).Select(...)` に書いた集計（例: OHLC）が、そのまま仕様になる
- 欠損埋め（任意）: 連続化が必要な場合だけ `WhenEmpty` を書く
- 出力: 実行側プロファイルで live/final の物理化・命名を決める（DSL には出ない）

要点（前提）
- すべての上位足は 1s_final からフラットに派生します（5m→15m の多段は使用しません）。
- grace は「親 + 1 秒」で段階的に増やします（遅延到着を確実に取り込みます）。
- Table は Streamiz により RocksDB へマテリアライズされ、`ToListAsync()` で参照できます。

最小の書き方（順番）
- TimeFrame → Tumbling → GroupBy → Select →（必要なら）WhenEmpty

構成の順番
- TimeFrame → Tumbling → GroupBy → Select →（必要なら）WhenEmpty

ポイント
- TimeFrame: 営業時間の拘束が必要なときだけ。日足以上は `dayKey` を付ける
- Tumbling: minutes/hours/days/months をまとめて指定できる
- GroupBy: 主キー（例: Broker, Symbol）
- Select: 集計仕様そのもの（ここに書いた内容が真実）
- WhenEmpty: 欠損埋めをしたいときだけ書く

``` mermaid
flowchart TB
  %% 入力とスケジュールフィルタ
  subgraph Upstream["上流（取引時間外の除外）"]
    raw["<raw>"]
    filtered["<raw>_filtered\n(取引時間外を除外)"]
    raw --> filtered
  end

  %% DSL レイヤ
  subgraph DSL["C# アプリケーション / DSL"]
    TF["TimeFrame<MarketSchedule>\n(dayKey: MarketDate など)"]
    Tumble["Tumbling\n(複数足を一括生成)"]
    GroupBy["GroupBy(主キー)"]
    Select["Select(OHLC 等の仕様)"]
  end

  filtered --> TF
  TF --> Tumble --> GroupBy --> Select

  %% 1s_final ハブ（唯一の親）
  subgraph Hub["確定 1 秒足ハブ"]
    final1s["bar_1s_final (TABLE)"]
    final1s_s["bar_1s_final_s (STREAM)\n※ 上位足の唯一の親入力"]
    final1s --> final1s_s
  end

  Select -->|DDL/CSAS/CTAS| final1s

  %% 上位足（すべて 1s_final_s からフラット派生）
  subgraph Live["上位足（live系: EMIT CHANGES）"]
    m1["bar_1m_live"]
    m5["bar_5m_live"]
    m15["bar_15m_live"]
    h1["bar_1h_live"]
    d1["bar_1d_live"]
    w1["bar_1w_live"]
  end

  final1s_s --> m1
  final1s_s --> m5
  final1s_s --> m15
  final1s_s --> h1
  final1s_s --> d1
  final1s_s --> w1

  %% 参照先（省略形）
  subgraph Storage["ローカルキャッシュ"]
    streamiz["Streamiz"]
    rocks["RocksDB 状態ストア"]
    streamiz --> rocks
  end
  m1 --> streamiz
  m5 --> streamiz
  m15 --> streamiz
  h1 --> streamiz
  d1 --> streamiz
  w1 --> streamiz

  %% コメント用の注釈
  classDef hint fill:#fffbe6,stroke:#f0e6a0,color:#333;


``` 

## 2. 処理の詳細（ここから深掘り）

### 2.1 TimeFrame と dayKey（営業日の境界）
```csharp
.TimeFrame<MarketSchedule>((r, s) =>
       r.Broker == s.Broker
    && r.Symbol == s.Symbol
    && s.Open <= r.Timestamp && r.Timestamp < s.Close,
    dayKey: s => s.MarketDate)
```
運用のコツ
- スケジュール判定は上流で実施します（例: `<raw>_filtered` を作成して参照します）。
- `dayKey` は「日/週/月などの境界を安定させる」ためのマーカーです。
- 分/時間足では原則不要です（指定しても構いません）。

### 2.2 Tumbling（複数足をまとめて宣言）
```csharp
.Tumbling(r => r.Timestamp, 
    Minutes = new[]{ 5, 15, 30 },
    Hours   = new[]{ 1, 4, 8 },
    Days    = new[]{ 1, 7 },
    Months  = new[]{ 1, 12 }
, grace: TimeSpan.FromMinutes(2))
```
使いどころ
- 1 回の宣言で複数の足をまとめて指定できます。
- grace は実行側の解釈に委ねます（内部では「親 + 1 秒」で伝播します）。
- 中間足や BaseUnit は非公開です（利用者が意識する必要はありません）。

### 2.3 GroupBy（主キー）
```csharp
.GroupBy(r => new { r.Broker, r.Symbol })
```
主キーの考え方
- GroupBy キー + バケット列（WindowStart）が主キーになります。

### 2.4 Select（投影＝仕様）
```csharp
.Select(g => new {
    g.Key.Broker,
    g.Key.Symbol,
    g.WindowStart(),                    // ← バケット列（“式”で認識、列名は任意）
    Open  = g.EarliestByOffset(x => x.Bid),
    High  = g.Max(x => x.Bid),
    Low   = g.Min(x => x.Bid),
    Close = g.LatestByOffset(x => x.Bid)
})
```
作るときの注意
- `g.WindowStart()` を必ず 1 回投影してください（列名は任意、式で識別します）。
- OHLC などの定義はアプリ側で明示してください（固定ではありません）。
- 派生段の投影は SELECT *（恒等）です。列名の固定や属性依存は行いません。

### 2.5 WhenEmpty（必要なときだけ・欠損埋め）
```csharp
.WhenEmpty((previous, next) =>
{
    next.Broker = previous.Broker;
    next.Symbol = previous.Symbol;
    next.Open   = previous.Close;
    next.High   = previous.Close;
    next.Low    = previous.Close;
    next.Close  = previous.Close;
    return next;
})
```
ポイント
- WhenEmpty を記述したときだけ「連続化モード」になります（HB + LEFT JOIN + Fill）。
- 記述しなければ疎のままです（デンス化しません）。
- 欠損埋めの結果を上流（final）へ戻さないでください（循環禁止）。

### 2.6 Table キャッシュと ToListAsync（RocksDB）
- Table は Streamiz により RocksDB にマテリアライズされます（StateStore）。
- `ToListAsync()` は「RUNNING 待ち → ストア全件列挙」を実行します。
- 前方一致フィルタは「NUL 区切りの文字列キー」で実現します。
- 伝達時間の目安は、通常 50〜200ms、起動直後は 0.5〜3 秒です（環境依存）。
- Stream ソースは `ToListAsync()` 非対応です（Push 購読を使用します）。

---

## 3. 内部の前提（知っておくと安心）
- 1s ハブ（= 1s_final）からフラットに派生します（5m→15m の多段は禁止です）。
- BaseUnitSeconds は 60 の約数のみ有効です（内部で自動展開します）。
- WindowStart は式で識別します（列名には依存しません）。
- 実行モードや物理名はプロファイル側で決定します（DSL では非公開です）。
- 欠損埋めの循環は禁止です（下流→上流へ戻しません）。
- grace は「親 + 1 秒」で階段的に伝播します。


---

## 4. バリデーション（自動チェック）
- BaseUnitSeconds は 60 の約数
- ウィンドウは BaseUnitSeconds の倍数（1m 以上は分の整数倍）
- grace は「親+1秒」を満たす
- よくあるエラー
  - Base unit must divide 60 seconds.
  - Windows ≥ 1 minute must be whole-minute multiples.
  - Windowed query requires exactly one WindowStart() in projection.

---

## 5. 代表シナリオ（複数足を一括生成）
- 秒/分/時間/日/月を一括宣言（1s_final ハブに一本化）
- 欠損埋めが必要な時だけ WhenEmpty を付ける

---

## 6. 拡張ポイント
- Aggregation Policy（例: VWAP, Volume, Trades）
- MarketSchedule（dayKey = MarketDate など）
- 命名/物理化は実行プロファイルで管理（DSL には出さない）

---

## 7. テストの観点（サクッと）
- `WindowStart()` が1回だけ含まれるか
- バリデーション（BaseUnit、倍数、分単位、循環検出）
- 合成ロジックの一貫性（1m→上位）
- 日足以上は dayKey の境界そろえ

---

## 8. 禁則（NG 集）
- `.EmitChanges()` / `.AsFinal()` など内部モードを匂わせない
- `.ToSink("…")` など物理名を DSL に露出しない
- 5m→15m の多段ロールアップは禁止（常に 1s_final から）
- 確定系列に Hopping を混在させない（速報系は別DAGに）

## 9. 命名規約（覚えどころ）

- **テーブル/トピック名**: `<entity>_<timeframe>_(live|final)`
  - 例: `bar_1s_final`, `bar_1m_live`, `bar_5m_live`, `bar_1d_live`
  - timeframe: `s`=秒, `m`=分, `h`=時間, `d`=日, `mo`=月
  - live/final: 集計モードの明示
  - filteredraw/nontrading_raw: `<raw_stream>_filtered` を参照（生成は上流責務）
- 1s_final は全上位足の唯一の親

1s_final / 1s_final_s（役割）
- 1s_final: EMIT FINAL の 1 秒確定足（TABLE）
- 1s_final_s: 1s_final を STREAM 化した入力専用の親
- ルール: 上位足は常に `<entity>_1s_final_s` を入力にする

---

## 10. 付録: 最小サンプル（コピペで雰囲気を掴む）
```csharp
EventSet<Rate>()
  .From<DeDupRates>()
  .ToQuery(q => q
    .TimeFrame<MarketSchedule>((r, s) =>
           r.Broker == s.Broker
        && r.Symbol == s.Symbol
        && s.Open <= r.Timestamp && r.Timestamp < s.Close,
        dayKey: s => s.MarketDate)

    .Tumbling(r => r.Timestamp, new Windows {
        Minutes = new[]{ 5, 15, 30 },
        Hours   = new[]{ 1, 4, 8 },
        Days    = new[]{ 1, 7 },
        Months  = new[]{ 1, 12 }
    }, grace: TimeSpan.FromMinutes(2))

    .GroupBy(r => new { r.Broker, r.Symbol })

    .Select(g => new {
        g.Key.Broker,
        g.Key.Symbol,
        g.WindowStart(),
        Open  = g.EarliestByOffset(x => x.Bid),
        High  = g.Max(x => x.Bid),
        Low   = g.Min(x => x.Bid),
        Close = g.LatestByOffset(x => x.Bid)
    })

    //.WhenEmpty((prev, next) => { /* 任意で欠損埋め */ return next; })
  );
```

> 実行モード（live/final）や命名/物理化は実行プロファイルで決定（DSL には出さない）

