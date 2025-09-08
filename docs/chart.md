# 足生成DSL・ロールアップ統一仕様（Codex向けコンパクト版）

## 0. 目的
- Tick（レート）から等間隔の足（分・時間・日・週・月）を生成する。
- **単一のクエリ**で複数の時間足をまとめて宣言できるようにする。
- 内部の中間足（例: 1m ハブ）や実行モード（live/final）などの内部実装は**外部に露出しない**。

---

## 1. 外部仕様（ユーザーが書くもの）
### 1.1 Query 構成順序（必須）
```
TimeFrame → Tumbling → GroupBy → Select → (WhenEmpty?)
```
- **TimeFrame**: 営業時間拘束が必要なときのみ。日足以上を作る場合は `dayKey` を指定。
- **Tumbling**: 1回の呼び出しで複数の足（minutes/hours/days/months）を**まとめて宣言**。
- **GroupBy**: 主キーに相当（例: Broker, Symbol）。
- **Select**: 投影＝仕様。ここに書いた集計がそのまま合成ロジックになる。
- **WhenEmpty**: 欠損埋め（連続化）を行いたい場合のみ記述（任意ラムダ）。

### 1.2 TimeFrame と dayKey（営業日境界のしるし）
```csharp
.TimeFrame<MarketSchedule>((r, s) =>
       r.Broker == s.Broker
    && r.Symbol == s.Symbol
    && s.Open <= r.Timestamp && r.Timestamp < s.Close,
    dayKey: s => s.MarketDate)
```
- `dayKey` は **日足以上（days, months）**で営業日境界を与える **マーカー**。
- 分足・時間足では指定不要。指定しても害はないが必須ではない。

### 1.3 Tumbling（複数足をまとめて）
```csharp
.Tumbling(r => r.Timestamp, new Windows {
    Minutes = new[]{ 5, 15, 30 },
    Hours   = new[]{ 1, 4, 8 },
    Days    = new[]{ 1, 7 },
    Months  = new[]{ 1, 12 }
}, grace: TimeSpan.FromMinutes(2))
```
- **1回の宣言で複数の足**をまとめて指定。
- `grace` は値を保持するだけ（実際の使われ方＝確定タイミングは実行レイヤが解釈）。
- **内部の中間足（例: 1m）や BaseUnit は非公開**。ユーザーは意識しない。

### 1.4 GroupBy（主キー）
```csharp
.GroupBy(r => new { r.Broker, r.Symbol })
```
- GroupBy キー ＋ バケット列（WindowStart）が主キーとなる。

### 1.5 Select（投影＝仕様）
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
- **`g.WindowStart()` を必ず1回投影**する（列名は任意、式で識別）。
- Open/High/Low/Close などの合成ロジックは **この投影に書いた内容が真実**。固定ではない（アプリ依存）。

### 1.6 WhenEmpty（任意ルールの欠損埋め）
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
- これを**書いた場合のみ**内部で「連続化モード」（HB + LEFT JOIN + Fill）が有効になる。
- 書かなければ疎なまま（デンス化なし）。
- **注意**: 欠損埋め結果は**下流専用**。最終確定（final）へ戻さない（循環禁止）。

---

## 2. 内部契約（ユーザーに見せないが、Codexが前提にするもの）
- **1m ハブ**: すべての上位足（5m/15m/…/1h/1d/1mo）は **1m からフラット派生**。多段ロールアップ禁止（5m→15m 等はしない）。
- **BaseUnitSeconds**: ユーザー指定可能。ただし **60 の約数**のみ有効。内部で Base→1m→上位のDAGを自動展開する。
- **WindowStart（バケット列）**: 投影内の `g.WindowStart()`“式”をキーに認識し、名前に依存しない。DDL/PK では SemanticRole=BucketStart として扱う。
- **実行モードの外出し**: live/final、確定タイミング（grace の解釈）、物理化・命名は実行レイヤ（プロファイル設定）で決定。DSLには現れない（`.EmitChanges()` 等は存在しない）。
- **循環禁止**: 欠損埋めや prev などの下流ビューを上流（final）へ戻さない。

---

## 3. バリデーション（ビルド時/実行時）
- **BaseUnitSeconds**: `60 % base == 0` を必須。
- **ウィンドウ指定**: すべて **BaseUnitSeconds の倍数**。かつ **1m 以上は分単位の整数倍**。
- **WindowStart**: ウィンドウ系クエリでは **必投影**／**重複投影は禁止**。
- **PK整合**: `GroupByキー + WindowStart(Role)` が主キーへ反映されることを検証。
- **循環検出**: 上流（final）への逆流配線を拒否。
- 代表エラー文言:
  - `Base unit must divide 60 seconds.`
  - `Window 7s must be a multiple of base 5s.`
  - `Windows ≥ 1 minute must be whole-minute multiples.`
  - `Windowed query requires exactly one WindowStart() in projection.`

---

## 4. 代表シナリオ（1クエリで複数足）
- 分・時間・日・月を**一括宣言**。中間足（1m）や実行モードは非公開のまま内部で展開。
- 欠損埋めをしたい場合のみ `WhenEmpty` を添える。しない場合は省略。

---

## 5. 拡張ポイント
- **Aggregation Policy**: 投影生成を差し替え可能（VWAP, Volume, Trades, など）。
- **MarketSchedule**: 取引所別の営業日・時間帯。`dayKey` は `MarketDate` などの営業日識別子。
- **命名/物理化**: 命名ポリシー・物理化有無・出力モードは実行プロファイルで管理（DSL には出さない）。

---

## 6. テスト指針
- **投影アナライザ**: `WindowStart()` 検出、1回制約、列名非依存の Role 付与を確認。
- **バリデーション**: BaseUnitSeconds／倍数制約／1m以上は分単位／循環検出。
- **合成の健全性**: 1m→上位の合成（投影に書いた関数）の関数結合性を単体テスト化。
- **日足以上**: `dayKey` による境界整列（週・月）を確認。

---

## 7. 禁則
- `.EmitChanges()` や `.AsFinal()` 等、内部モードを匂わせるAPIは**存在しない**。
- `.ToSink("…")` 等、物理名を DSL に露出させない。
- 5m→15m のような多段ロールアップは禁止（常に 1m からフラット派生）。

---

## 付録: 最小サンプル（概念）
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

> 実行モード（live/final）、命名/物理化は**実行プロファイル**で決定。DSLには現れない。

