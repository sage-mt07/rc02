# 足生成DSL・ロールアップ統一仕様（Codex向けコンパクト版）

## 0. 目的
- Tick（レート）から等間隔の足（秒・分・時間・日・週・月）を生成する。
- **単一のクエリ**で複数の時間足をまとめて宣言できるようにする。
- 内部の中間足（例: 1s ハブ / 1s_final）や実行モードは**外部に露出しない**（プロファイルで決定）

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
- **実行レイヤでの原則**：マーケットスケジュールによる取引時間判定は Raw 取り込み直後（または最上流）で付与し、`IsTrading` による **filteredraw** を生成してから集計する（DSL外の責務）
- `dayKey` は **日足以上（days, months）**で営業日境界を与える **マーカー**。
- 分足・時間足では原則不要（指定可）。スケジュール判定自体は上流ガードで行う。

### 1.3 Tumbling（複数足をまとめて）
```csharp
.Tumbling(r => r.Timestamp, 
    Minutes = new[]{ 5, 15, 30 },
    Hours   = new[]{ 1, 4, 8 },
    Days    = new[]{ 1, 7 },
    Months  = new[]{ 1, 12 }
, grace: TimeSpan.FromMinutes(2))
```
- **1回の宣言で複数の足**をまとめて指定。
- `grace` は**実行レイヤ解釈**のメタ情報。内部では **階段ルール（親+1秒）**で伝播させる。
- **内部の中間足（1sハブ=1s_final）や BaseUnit は非公開**。ユーザーは意識しない。

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
-  **1s ハブ（= 1s_final）**: すべての上位足（1m/5m/…/1h/1d/1mo）は **1s_final からフラット派生**。多段ロールアップ禁止（5m→15m 等はしない）。
- **BaseUnitSeconds**: ユーザー指定可能。ただし **60 の約数**のみ有効。内部で Base→**1s_final**→上位のDAGを自動展開する。
- **WindowStart（バケット列）**: 投影内の `g.WindowStart()`“式”をキーに認識し、名前に依存しない。DDL/PK では SemanticRole=BucketStart として扱う。
- **実行モードの外出し**: live/final、確定タイミング（grace の解釈）、物理化・命名は実行レイヤ（プロファイル設定）で決定。DSLには現れない（`.EmitChanges()` 等は存在しない）。
- **運用規約*: 分足・上位足の**確定系（final）を別途持たず**、`live(TUMBLING, EMIT CHANGES)` を唯一の足として扱い、必要に応じて「確定フラグ（IsClosed）」を算出列で提供可。
- **循環禁止**: 欠損埋めや prev などの下流ビューを上流（final）へ戻さない。
- **Grace伝播の階段ルール**: `Grace(上位) = Grace(親) + 1秒`（例：1s=3s→1m=4s→5m=5s）。  
- これにより 1s_final の遅延確定を上位が確実に取り込む。


---

## 3. バリデーション（ビルド時/実行時）
- **BaseUnitSeconds**: `60 % base == 0` を必須。
- **ウィンドウ指定**: すべて **BaseUnitSeconds の倍数**。かつ **1m 以上は分単位の整数倍**。
- **Grace整合**: 生成する各ウィンドウに対し、**親のGrace +1秒**を満たすこと（親=1s_final）。
- 代表エラー文言:
  - `Base unit must divide 60 seconds.`
  - `Window 7s must be a multiple of base 5s.`
  - `Windows ≥ 1 minute must be whole-minute multiples.`
  - `Windowed query requires exactly one WindowStart() in projection.`

---

## 4. 代表シナリオ（1クエリで複数足）
- 秒・分・時間・日・月を**一括宣言**。中間足は **1s_final ハブ**に一本化し、実行モードは非公開のまま内部で展開。
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
- 5m→15m のような多段ロールアップは禁止（常に **1s_final** からフラット派生）。
- **Hopping を確定系列に混在させない**（速報用は別系統・別DAGでのみ許容）。

## 8. 命名規約

- **テーブル/トピック名**: `<entity>_<timeframe>_(live|final)`
  - 例: `bar_1s_final`, `bar_1m_live`, `bar_5m_live`, `bar_1d_live`
- **timeframe表記**: `s`=秒, `m`=分, `h`=時間, `d`=日, `mo`=月
- **live/finalの接尾辞**: 集計モードを明示する
- **filteredraw/nontrading_raw**: 取引時間フィルタ済/除外用の特別ストリーム <raw_stream_name>_filtered
- **1s_final**: 全上位足の唯一の親。必ず存在

1s_final / 1s_final_s の役割
- 1s_final: EMIT FINAL を用いた 1 秒確定足。TABLE として保持し、確定値を保証する。
- 1s_final_s: 1s_final を STREAM 化したもの。上位足生成の唯一の親として利用する。
- ルール: 上位足は必ず *_1s_final_s を入力にする。
- 各ウィンドウは前段の足を参照せず、常に `<entity>_1s_final_s` を入力とする。

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

