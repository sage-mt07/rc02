# 足生成DSL仕様（たたき台）

## 目的
- 金融レートデータ（Rate）から **等間隔足（1分〜月足）** を生成する。
- マーケットスケジュール（MarketSchedule）に基づいて、営業日・営業時間内に限定する。
- 学習コストを抑えるため、**予約語は増やさない**。

--

## 設計方針
1. **等間隔の区切り**は `.Tumbling` で表現  
   - 粒度（minutes, hours, days, months）を指定可能  
   - 遅延到着に対応するため `grace` を設定可能（ウォーターマーク相当）  
   - 欠損バケットを埋める場合は `ensureContinuous: true`

2. **マーケットスケジュール結合**は `.TimeFrame<MarketSchedule>`
   - 引数は **結合条件式のみ**
   - Open/Close の包含判定もここで明示する
   - デフォルト結合キーや暗黙ルールは存在しない
   - 呼び出し順序は `.TimeFrame().Tumbling()` の連続を仕様とし、型で強制する

3. **集約は GroupBy + 集計関数**  
   - `GroupBy` で Broker, Symbol, BucketStart をキーにする  
   - `Select` 内で **EarliestByOffset / LatestByOffset / Min / Max** を利用して OHLC を表現  
   - Count など不要な集計は記述しない

4. **Key の扱い**  
   - C#側では GroupBy のキーは匿名型／値タプル  
   - ksql では GROUP BY の列が KEY列になる  
   - 「GroupBy 時間列 = Tumbling に渡した列」は **バケット開始に丸められる**ことを仕様で保証する

---

public class Rate
{
   [KsqlKey(1)]
    public string Broker { get; set; }
   [KsqlKey(2)]
    public string Symbol { get; set; }
   [KsqlKey(3)]
    public DateTime BucketStart { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
}
## DSLシンタックス（イメージ）

```csharp



パターン１
```csharp
EventSet<Rate>()
  .From<DeDupRates>()
  .ToQuery(q => q
    .Tumbling(r => r.Timestamp,
              minutes: new[]{1,5,15,30},
              hours:   new[]{1,4,8},
              days:    new[]{1,7},
              months:  new[]{1,12},
              grace: TimeSpan.FromMinutes(2)) // 遅延許容

    .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })

    .Select(g => new {
        g.Key.Broker,
        g.Key.Symbol,
        g.Key.BucketStart,
        Open  = g.EarliestByOffset(x => x.Bid),
        High  = g.Max(x => x.Bid),
        Low   = g.Min(x => x.Bid),
        Close = g.LatestByOffset(x => x.Bid)
    })
  );

この場合、KSQLのTumblingのみの処理
minutes: new[]{1,5,15,30},
              hours:   new[]{1,4,8},
              days:    new[]{1,7},
              months:  new[]{1,12},
              
            この指示内容の足用topicを作成する
            それはRate_1m_final,Rate_1d_finalとかになる


パターン２
EventSet<Rate>()
  .From<DeDupRates>()
  .ToQuery(q => q
    .TimeFrame<MarketSchedule>((r, s) =>
         r.Broker == s.Broker
      && r.Symbol == s.Symbol
      && s.Open <= r.Timestamp && r.Timestamp < s.Close,
      dayKey: s => s.MarketDate)
      // TimeFrame → Tumbling の順序は必須
    .Tumbling(r => r.Timestamp,
              minutes: new[]{1,5,15,30},
              hours:   new[]{1,4,8},
              days:    new[]{1,7},
              months:  new[]{1,12},
              ensureContinuous: true,
              grace: TimeSpan.FromMinutes(2)) // 遅延許容
      .WhenEmpty((previous,next)=>
      next.Broker=previous.Broker,
      next.Symbol=previous.Symbol,
      next.Open=previous.Close,
      next.High=previous.Close,
      next.Low=previous.Close,
      next.Close=previous.Close,
      )


    .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })

    .Select(g => new {
        g.Key.Broker,
        g.Key.Symbol,
        g.Key.BucketStart,
        Open  = g.EarliestByOffset(x => x.Bid),
        High  = g.Max(x => x.Bid),
        Low   = g.Min(x => x.Bid),
        Close = g.LatestByOffset(x => x.Bid)
    })
  );

この指示内容の足用topicを作成する
Rate_1m_live,Rate_1d_finalとかになる
Rate_1m_final,Rate_1d_finalとかになる

ensureContinuousがHBを示す
Tumbling　で示す　　Timestamp
 TimeFrameの　Timestamp　と比較対象を利用しHBの開始、終了とする

この組み合わせで live finalのtopicを作る
HBでliveとfinalへデータ送信する

内部の仕組み


10secごとに足を編集する
[Tick(≈1ms) / DeDupRates]
   |  (原始レート: Broker, Symbol, Timestamp, Bid)
   v
+--------------------------------------------+
| bar_10s_agg_final  (EMIT FINAL, GRACE)     | ① 10秒確定集約：HL完全捕捉
|  (B,S,BucketStart, O,H,L,C)                |
+---------------------------+----------------+
                            |
                            |(10sごとにライブ化；空でも出すためHB)
                            v
                  +--------------------+
                  | HB_10s (C#送信)   | ② 10秒ドライバ（唯一のApp責務）
                  | (B,S,BucketStart) |
                  +----+---------------+
                       |
                       | ③ 10s live（EMIT CHANGES）
                       v
                +----------------------+
                | bar_10s_live         |
                | (B,S,BucketStart,    |
                |  O,H,L,C)            |
                +----+-----------------+
                     |
                     | ④ ロールアップ（TUMBLING）
                     v
         +----------------------+              +----------------------+
         | bar_1m_live          | ⑤ 1分live   | bar_5m_live          | ⑥ 5分live
         | (O=Earliest,         | (EMIT CHG)  | (EMIT CHG)           |
         |  H=Max, L=Min,       |             |                      |
         |  C=Latest)           |             |                      |
         +----------------------+             +----------------------+

                                （final系はHB駆動・non-null保証）
                                ──────────────────────────────────
         +----------------------+             +----------------------+
         | HB_1m (派生:10s→1m) | ⑦           | HB_5m (派生:10s→5m) | ⑧
         +----------+-----------+             +----------+-----------+
                    |                                     |
                    | ⑨ 1分確定集約 (EMIT FINAL, GRACE)   | ⑪ 5分確定集約 (EMIT FINAL, GRACE)
                    v                                     v
           +---------------------+               +---------------------+
           | bar_1m_agg_final    |               | bar_5m_agg_final    |
           +----------+----------+               +----------+----------+
                      |                                     |
                      | ⑩ prev_1m（直近確定の保持：B,S）     | ⑫ final生成（prev_1mで欠損埋め）
                      v                                     v
           +---------------------+               +---------------------+
           | bar_prev_1m         |               | bar_5m_final        |
           | (B,S, Close[+OHL])  |               | (HB_5m×agg×prev_1m) |
           +----------+----------+               +---------------------+
                      |
                      | ⑬ final生成（prev_1mで欠損埋め）
                      v
           +---------------------+
           | bar_1m_final        |
           | (HB_1m×agg×prev_1m) |
           +---------------------+

役割分担（再確認）

C#（アプリ）：HB_10s の送信のみ（全銘柄へ10秒ごとに (Broker,Symbol,BucketStart) を発火）

ksqlDB：

集約：bar_10s_agg_final / bar_1m_agg_final / bar_5m_agg_final（すべて EMIT FINAL + GRACE）

ライブ：bar_10s_live（HB_10s 駆動）→ bar_1m_live → bar_5m_live（ロールアップ）

確定：bar_1m_final / bar_5m_final（HB × agg_final × prev_1m で non-null を保証）

前回値：bar_prev_1m（1mのみ保持）

不変ルール

HLは10sで完全捕捉（Max/Min）→ 上位TFはロールアップでもHLは失われない

liveは10s基準：10s→1m→5m を EMIT CHANGES で段階更新

finalはHB駆動：空バケット抑止は キー存在判定（a.Broker IS NOT NULL OR prev/final.Broker IS NOT NULL）

prevは1mのみ：全TFの final が prev_1m をフォールバック参照

POCOはnon-nullable：nullは SQL の COALESCE + WHERE で外へ出さない

派生HB：HB_1m/5m は HB_10s から間引き（MOD(… , frameMs)=0）

月サフィックスは mo（mとの衝突回避）           

┌──────────────────────────────────────────────────────────────┐
│ 1) スケジュール準備（オフライン/起動時）                    │
│   - 取引カレンダーをロード：祝日/臨時休場/短縮/メンテ       │
│   - 営業時間セッションを列挙：Open/Close（含み方も規約化）  │
│     規約:  Open <= t < Close                                 │
│   - タイムゾーン/DST/夏時間補正                              │
│   - alignOffsetMs を市場・銘柄単位で算出                     │
│     例: 東京 09:00 開始 → UTC ミリ秒オフセットを前計算       │
└──────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 2) HB_10s 生成（C#、唯一のアプリ責務）                       │
│   - 監視対象 (Broker, Symbol) を列挙                         │
│   - 現在の時刻 t を MarketSchedule と突合                    │
│     ・t が営業セッション内なら 10秒境界に整列し HB_10s を送信│
│       （Broker, Symbol, BucketStart）                        │
│     ・t が休場/休憩/メンテなら HB を送らない                 │
│   - セッション境界での振る舞い                               │
│     ・Open 時刻：Open に整列した HB を**必ず**送る           │
│     ・Close 時刻：Close に“到達前まで”送る（Open<=t<Close）  │
│   - 複数セッション（昼/夜）対応：各セッションで同処理        │
│   - 特例（短縮/臨時）：スケジュールの Open/Close をそのまま適用│
└──────────────────────────────────────────────────────────────┘
             │（HB_10s は“営業セッション内の10秒刻み”だけが出る）
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 3) 派生 HB（ksqlDB）                                         │
│   - HB_1m / HB_5m を HB_10s から間引き                        │
│     MOD((BucketStartMs - alignOffsetMs), frameMs) = 0         │
│   - 営業時間外は HB_10s が無い → 派生HBも出ない              │
└──────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 4) 10s ライブ（ksqlDB, EMIT CHANGES）                         │
│   - HB_10s × bar_10s_agg_final × bar_1m_final（fallback）     │
│   - 営業時間外は HB が無い → ライブも出ない                  │
│   - 遅延到着は GRACE 内で同一 10s バケットを上書き           │
└──────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 5) ライブのロールアップ（ksqlDB, EMIT CHANGES）               │
│   - bar_1m_live = 10s_live の TUMBLING(1m)                    │
│   - bar_5m_live = 1m_live  の TUMBLING(5m)                    │
│   - 営業時間外は上流にイベント無し → 何も出ない              │
└──────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 6) 確定集約（ksqlDB, EMIT FINAL + GRACE）                     │
│   - bar_10s_agg_final / bar_1m_agg_final / bar_5m_agg_final   │
│   - 営業セッション内の Tick のみが対象                        │
│   - GRACE 過ぎで確定（遅延取り込み後、値は不変）             │
└──────────────────────────────────────────────────────────────┘
             │
             ▼
┌──────────────────────────────────────────────────────────────┐
│ 7) prev と final（ksqlDB、non-nullable保証）                  │
│   - prev は 1m のみ：bar_prev_1m = LATEST_BY_OFFSET(Close)   │
│     ・日またぎ/セッションまたぎの初回バー：                  │
│       ― 初回は final と prev が一致するよう移行シード/T₀運用 │
│   - final(1m) = HB_1m × 1m_agg_final × prev_1m               │
│   - final(5m) = HB_5m × 5m_agg_final × prev_1m               │
│   - 空バケット抑止：WHERE a.Key IS NOT NULL OR prev.Key IS NOT NULL │
│   - 営業時間外は HB 無 → final も出ない                      │
└──────────────────────────────────────────────────────────────┘
MarketSchedule で決めるべき規約（明文化）

包含規則：Open <= t < Close

Close ちょうどの時刻は含めない（次セッションの開始と衝突しないため）。

整列オフセット（alignOffsetMs）

市場起点（例：9:00, 8:45 など）に 10s/1m/5m の境界を同期。

ksql 派生HBの MOD((BucketStartMs - alignOffsetMs), frameMs)=0 で全TFを揃える。

休場/休憩/臨時

HB そのものを止める（「出さないこと」で全下流が静止）。

これによりライブ/ファイナルも自動的に出ず、NULL 行も発生しない。

セッション開始の初回バー

原則「前回の確定値（prev_1m）」で欠損埋め可能にしておく（移行シード/T₀ ルール）。

これで 初回 final と prev が一致（要件どおり）。

日足・月足・営業日足

同じ仕組みで HB 日次/営業日次を作る（MarketSchedule の営業日テーブルから HB を発火）。

月足は mo サフィックス、営業日境界はスケジュール由来の alignOffsetMs で管理。

TimeFrame<MarketSchedule> の扱い

DSL では検証のみ（Open/Close の包含、Broker/Symbol の一致、TradingDate 算出）。

SQL へは持ち込まない（HB がスケジュール順守で生成される前提）。

**TimeFrame を省略した場合**、`Day()`/`Week()`/`Month()` は UTC 暦で解釈され、`Minutes`/`Hours` はそのままの時間幅で扱われる。

`Week(DayOfWeek.Monday)` や `Month()` は、TimeFrame に `dayKey` を指定した場合、その `dayKey` が示す営業日集合から境界を導出する。

想定ユースケース別の動き

短縮取引日：Close が早まる → HB 停止が早まる → 集約窓もそこで止まる。

昼休み：休憩帯は HB を出さない → ライブもファイナルも沈黙。

DST 切替：スケジュール側で時刻解決 → alignOffsetMs に反映 → 全 TF の境界が自動同期。

市場横断：Broker/Symbol 単位で別 MarketSchedule を持てる。HB 送信は対象ごとに判定。


足生成DSL + MarketSchedule 開発リファレンス
1. 全体像（更新は10秒単位）

Tick (≈1ms) → 10s 集約 (agg_final) → HB_10s 駆動 → 10s live

10s live → 1m live → 5m live（ロールアップ）

各TFの final は HB駆動 + agg_final + prev_1m で non-nullable 保証

prev は 1m のみ保持し、全TFの欠損埋めに利用

2. 役割分担
担当	責務	実装
C# (App)	- HB_10s の送信（唯一の役割）
- POCO 定義（non-nullable, PK属性固定）
- MarketSchedule を参照して Open/Close 判定
- alignOffsetMs の計算	HB10s プロデューサ、EF Core ToQuery で POCO登録
ksqlDB	- Tick からの集約 (10s/1m/5m agg_final)
- prev_1m 管理
- final 生成（HB×agg_final×prev_1m）
- live 生成（10s HB駆動, 上位はロールアップ）
- 欠損埋め (COALESCE)
- 遅延処理 (GRACE + EMIT FINAL)	SQL定義（bar_agg_final, bar_prev_1m, barfinal, bar*_live）
3. タイムフレームごとのテーブル定義
粒度	agg_final	prev	final	live
10s	bar_10s_agg_final	–	–	bar_10s_live (HB_10s駆動)
1m	bar_1m_agg_final	bar_prev_1m	bar_1m_final	bar_1m_live (10s live ロールアップ)
5m	bar_5m_agg_final	– (参照: bar_prev_1m)	bar_5m_final	bar_5m_live (1m live ロールアップ)
日/月	bar_1d_agg_final / bar_1mo_agg_final	– (参照: bar_prev_1m)	bar_1d_final / bar_1mo_final	任意（必要ならロールアップ）

prev は 1m のみ保持。それ以上のTFはすべて prev_1m を参照して欠損埋め。

4. MarketSchedule に基づく制御

包含規則

Open <= t < Close （Close時刻は含まない）

HB生成ルール（C#側）

営業時間内のみ 10s 整列で送信

休場/昼休みは HB を送らない（下流も停止）

複数セッションは Open/Close ごとに判定

短縮・臨時は MarketSchedule に従う

alignOffsetMs

市場ごとの開始時刻を UTCエポックmsに換算して設定

すべてのTFは MOD((BucketStartMs - alignOffsetMs), frameMs)=0 で整列

5. 初回移行（T₀）ルール

移行直後、bar_prev_1m を T₀以前の Close でシード

T₀の最初の bar_1m_final が prev と一致することで、初回空バケットを回避

これにより finalとprevが一致してスタートする

6. エラー・遅延時の挙動

GRACE 内の遅延 → 同じバケットが更新され、値が修正される

GRACE 超過の遅延 → その Tick は捨てられ、チャートに反映されない

HB停止 → 休場/障害のどちらでも下流に何も出ない（null 行は発生しない）

7. 命名規約

bar_<tf>_agg_final / bar_<tf>_final / bar_<tf>_live

bar_prev_1m

HBトピック: HB_10s（C#送信）、HB_1m / HB_5m（派生）

サフィックス: m, h, d, mo（monthは mo）

補足：責務分離と時間キーの扱い
1. 時間キーの一貫性

Tumbling に渡した timestamp 列を「唯一の時間キー」とする。

TimeFrame の境界比較、GroupBy の時間列、HB の領域判定はすべて この列に統一する。

DSL/変換時に、この列が一致していない場合はエラーとする（静的検証ルール）。

2. TimeFrame と HB の責務分離

市場包含規則の真実源は TimeFrame。

HB 側では独自にロジックを持たず、TimeFrame で利用される Open/Close 値を参照するだけ。

これによりアプリコードと KSQL 側の判定が二重化せず、一貫性が担保される。

3. HB の役割限定

HB は「確定タイミングを指示するだけの時計役」。

値の生成ロジック（OHLC 集約や欠損埋め）は ksqlDB 側が担う。

アプリの唯一の責務は HB_10s の送信であり、それ以上のアプリコード生成を許容しない。

4. RocksDB と Final の関係（未明記部分）

RocksDB は live/final 双方の状態を同期する。

Final の確定は二経路存在：

Tumbling (EMIT FINAL + GRACE) による自動確定

HB 到来による強制確定

HB で確定する場合、値が無ければ prev トピックの値を使う。

5. prev の役割の一般化

bar_prev_1m は「直近確定値を保持し、全 TF の final にフォールバック値を与える」専用トピック。

prev を参照するのは final 生成時のみ。live 生成では使わない。

日またぎ／セッションまたぎの初回バーも、prev で埋めることで non-nullable を維持できる。

6. 検証と防波堤

Codex がアプリコード（例：スケジュール判定ロジック）を作らないように、

MarketSchedule の列を真実源とすること

Tumbling に渡した列がすべての判定に使われること

HB は時刻指示だけであること
をドキュメントに明記し、責務逸脱を禁止する。

ValueShape/KeyShape は POCO を唯一の真実源。Projection は表示ヒント。PKあり→TABLE既定。一致検証はハッシュ一回。
Value/Key は POCO に由来し、PK が指定された場合は TABLE が既定となる。HB は常に STREAM として扱われ、スキーマ整合性は PocoSchemaHash 単位で一度だけ検証される。Builder は WindowedQueryBuilder を中心とする Core へ集約され、各 Builder はそこへ委譲される。

Topics.* のキーは解決後のトピック名を用い、HB トピックも対象となるため短期 retention.ms の設定など運用調整が可能。
NullabilityInfoContext で検出するため init-only/readonly プロパティは ReadState になり得るほか、NRT 無効プロジェクトでは参照型がすべて非 null 扱いとなる。
