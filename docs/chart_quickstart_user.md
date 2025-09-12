# 足生成クイックスタート（利用者向け）
このガイドは Tick から複数タイムフレームの足を最短手順で生成するための実践メモです。

想定読者
- Tick から秒足〜月足を生成したい実務ユーザーを想定しています。
- 実装詳細よりも動かし方を知りたい方向けです。

できること（概要）
- Tick から複数タイムフレームの足を一括生成できます（例: 1m/5m/15m/1h/1d）。
- MarketSchedule（営業日カレンダー）と結合して、日/週の境界を安定させられます。
- 集計仕様として OHLC（始値・高値・安値・終値）などを Select に明示し、そのまま実行できます。
- Table を RocksDB（内部ストレージ）にマテリアライズし、
  `ToListAsync()` で高速に取得できます。

すぐ試す（最短 5 ステップ）
1) 接続先を設定する（事前準備）
- Kafka / ksqlDB / Schema Registry を起動しておきます。
- `appsettings.json` に最低限の接続先を設定します。
  - `KsqlDsl:Common:BootstrapServers`
  - `KsqlDsl:SchemaRegistry:Url`
  - `KsqlDsl:KsqlDbUrl`

2) 接続できる状態を作る（Context 作成）
```csharp
var cfg = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json") // 接続先設定を読み込む
  .Build();
var ctx = KsqlContextBuilder.Create()
  .UseConfiguration(cfg) // 読み込んだ接続設定を適用
  .BuildContext<MyAppContext>();
```

3) Rate をトピック "rates" に関連づける（モデル登録）
```csharp
[KsqlTopic("rates")]
public class Rate
{
  [KsqlKey(0)] public string Broker { get; set; } = "";
  [KsqlKey(1)] public string Symbol { get; set; } = "";
  [KsqlTimestamp] public DateTime Timestamp { get; set; }
  public double Bid { get; set; }
}
```

4) 日/週の境界を含む複数足を定義する（クエリ）
```csharp
modelBuilder.Entity<Bar>().ToQuery(q => q
  .From<Rate>()
  .TimeFrame<MarketSchedule>((r, s) =>
         r.Broker == s.Broker
      && r.Symbol == s.Symbol
      && s.Open <= r.Timestamp && r.Timestamp < s.Close,
      dayKey: s => s.MarketDate)
  .Tumbling(r => r.Timestamp, new Windows { Minutes = new[]{ 1, 5, 15 }, Days = new[]{ 1 } })
  .GroupBy(r => new { r.Broker, r.Symbol })
  .Select(g => new Bar {
    Broker = g.Key.Broker,
    Symbol = g.Key.Symbol,
    BucketStart = g.WindowStart(),
    Open  = g.EarliestByOffset(x => x.Bid),
    High  = g.Max(x => x.Bid),
    Low   = g.Min(x => x.Bid),
    Close = g.LatestByOffset(x => x.Bid)
  }));
```

5) 送受信を確かめる（動作確認）
- 送信します（Stream / Table 共通）。
```csharp
await ctx.Set<Rate>().AddAsync(new Rate {
  Broker = "B1", Symbol = "S1", Timestamp = DateTime.UtcNow, Bid = 100
});
```
- 受信します（Stream は `Push（購読方式）`）。
```csharp
await ctx.Set<Bar>().ForEachAsync(b => { Console.WriteLine(b.Symbol); return Task.CompletedTask; });
```
- 取得します（Table は RocksDB から `Pull（取得方式）` します）。
```csharp
var list = await ctx.Set<Bar>().ToListAsync();
```

使うときの注意点（まずここだけ）
以下は利用者が実装時に意識しておくと迷わない要点です。
- Select には g.WindowStart() を 1 回だけ入れます（重複はエラー）。
- 日足以上を作る場合は dayKey（同一営業日を識別するキー列）を付けます。
- すべての足は基底の1秒足から直接生成します（例: 5分足や15分足も1秒足から作ります）。
- 確定値（final）と速報値（live）は別DAG（処理の流れ＝依存グラフ）にします。
- 取得方式は、Table は `ToListAsync()`、Stream は `ForEachAsync` で購読します。
- 伝達時間は環境により変動します。通常は 50〜200ms、起動直後は 0.5〜3 秒が目安です。
上記を押さえたら、典型的な検証エラーと対処法も把握しておくとスムーズです。

自動チェックと対処のコツ（困ったら見る）
内部ルールの多くは自動で検証します。エラーが出たら次を確認してください。
- 「Windowed query requires exactly one WindowStart()」
  - 対処: Select に `g.WindowStart()` を 1 回だけ含めてください（重複や欠落に注意）。
- 「Windows ≥ 1 minute must be whole-minute multiples」
  - 対処: 1 分以上の窓サイズは 1 分単位の整数倍にしてください（例: 1m, 5m, 15m）。
- 「Windows must be multiples of the base unit」などの窓サイズ系エラー
  - 対処: 秒台の微妙なサイズ指定を避け、一般的なサイズ（1m/5m/15m/1h/1d など）を選んでください。
これで基礎的な検証エラーは解消できます。続いて命名規約も確認してください。

命名規約（代表）
- `<entity>_<timeframe>_(live|final)` の形式を使います（例: `bar_1m_live`, `bar_1d_live`）。
- timeframe は `s`=秒, `m`=分, `h`=時間, `d`=日, `mo`=月 です。
- 1s_final / 1s_final_s は上位足の唯一の親です。

トラブル対策（抜粋）
- 反映が遅い場合は、起動直後に数秒待機し、短いポーリングで再試行してください。
- 期待件数が足りない場合は、TimeFrame の条件と dayKey、そして WindowStart の投影を確認してください。
- `ToListAsync()` が例外になる場合は、対象が Stream の可能性があります。Table を対象にしてください。

参考
- 詳細は `docs/chart.md` を参照してください。

成功確認チェックリスト
- 送信後、`bar_1m_live` にレコードが出る。
- `ToListAsync()` で 0 件なら TimeFrame 条件と `g.WindowStart()` を再確認。
- `ksqlDB` の `SHOW TABLES` で `bar_1m_live` / `bar_1d_live` が見える。
