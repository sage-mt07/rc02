# 足生成クイックスタート（利用者向け）

想定読者
- Tick から秒足〜月足を生成したい実務ユーザーを想定しています。
- 実装詳細よりも動かし方を知りたい方向けです。

できること（概要）
- Tick から複数タイムフレームの足を一括生成できます（例: 1m/5m/15m/1h/1d）。
- 営業日カレンダー（MarketSchedule）と結合して、日/週の境界を安定させられます。
- 集計仕様（OHLC など）を Select に明示し、そのまま実行できます。
- Table を RocksDB にマテリアライズし、`ToListAsync()` で高速に取得できます。

すぐ試す（最短 5 ステップ）
1) 事前準備をします
- Kafka / ksqlDB / Schema Registry を起動しておきます。
- `appsettings.json` に最低限の接続先を設定します。
  - `KsqlDsl:Common:BootstrapServers`
  - `KsqlDsl:SchemaRegistry:Url`
  - `KsqlDsl:KsqlDbUrl`

2) コンテキストを作成します
```csharp
var cfg = new ConfigurationBuilder()
  .AddJsonFile("appsettings.json") // 接続先設定を読み込む
  .Build();
var ctx = KsqlContextBuilder.Create()
  .UseConfiguration(cfg) // 読み込んだ接続設定を適用
  .BuildContext<MyAppContext>();
```

3) モデルを登録します（使う型にトピックを結びつけます）
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

4) クエリを定義します（日/週の境界 + 複数足 + OHLC）
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

5) 動作を確認します
- 送信します（Stream / Table 共通）。
```csharp
await ctx.Set<Rate>().AddAsync(new Rate {
  Broker = "B1", Symbol = "S1", Timestamp = DateTime.UtcNow, Bid = 100
});
```
- 受信します（Stream は Push 購読）。
```csharp
await ctx.Set<Bar>().ForEachAsync(b => { Console.WriteLine(b.Symbol); return Task.CompletedTask; });
```
- 取得します（Table は RocksDB から Pull します）。
```csharp
var list = await ctx.Set<Bar>().ToListAsync();
```

使うときの注意点（まずここだけ）
以下は利用者が実装時に意識しておくと迷わない要点です。
- Select の投影に `g.WindowStart()` を一度だけ含めて、ウィンドウ開始時刻の列を出力してください。列名は自由です（例: `BucketStart = g.WindowStart()`）。複数回入れるとエラーになります。
- 日足以上を作る場合は、dayKey（例: MarketDate）を付けてください。
- 多段ロールアップは行わないでください（5m→15m ではなく、1s_final から派生させます）。
- 確定系列に Hopping を混在させないでください（速報用途は別 DAG に分けます）。
- 取得方式は、Table は `ToListAsync()`、Stream は Push（`ForEachAsync`）です。
- 伝達時間は環境により変動します。通常は 50〜200ms、起動直後は 0.5〜3 秒が目安です。

自動チェックと対処のコツ（困ったら見る）
内部ルールの多くは自動で検証します。エラーが出たら次を確認してください。
- 「Windowed query requires exactly one WindowStart()」
  - 対処: Select に `g.WindowStart()` を 1 回だけ含めてください（重複や欠落に注意）。
- 「Windows ≥ 1 minute must be whole-minute multiples」
  - 対処: 1 分以上の窓サイズは 1 分単位の整数倍にしてください（例: 1m, 5m, 15m）。
- 「Windows must be multiples of the base unit」などの窓サイズ系エラー
  - 対処: 秒台の微妙なサイズ指定を避け、一般的なサイズ（1m/5m/15m/1h/1d など）を選んでください。
※ BaseUnit や grace の詳細は内部で調整されます。通常は利用者が設定・調整する必要はありません。

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
