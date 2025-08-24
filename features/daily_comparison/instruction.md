[しおんへの開発タスク指示]
概要
ローカルPCのDocker環境で動作する2つのプロセスを含むシステムを構築してください。
主な要件は以下です。

要件詳細
1. レート送信プロセス（リアルタイム）

broker, symbol, bid, ask, rateid, ratetimestamp を含むレート情報をリアルタイムで送信・保存する。

主キーは broker, symbol, rateid。

2. MarketScheduleの管理

broker, symbol, date, opentime, closetime からなる市場スケジュールテーブルを日次で更新する。

市場ごとに開場・閉場時刻を保持すること。

3. 日足比較表の自動生成

日次で、「日足（high, low, close）」を集計し、前日終値（prev_close）と比較したdiffも計算する。

日足のhigh/low/closeは、marketscheduleのopentime～closetimeで集計すること。

DailyComparisonテーブルを用意し、以下の形式で保持する：

```
broker, symbol, date, high, low, close, prev_close, diff
```

4. 参照プロセス

別プロセスでDailyComparisonテーブルの内容を任意のタイミングで取得・表示できるようにする。

5. データストア

ローカルのmdfファイル(SQL Server LocalDB等)を利用し、全データを保存すること。

補足・技術制約
- C# (.NET 6 or 8) + Entity Framework Coreの利用を推奨
- 日足計算の際はmarketscheduleを参照し、取引時間外は集計しないこと
- テストコードも作成すること

[成果物]
- Docker Composeファイル（ローカル環境用）
- C#のソースコード一式
- SQLスキーマ定義（Entity定義でも可）
- サンプルテストデータ
- 実装・動作手順の簡単なREADME

[ゴール]
- レート送信プロセスがリアルタイムでレートを記録
- marketscheduleが日次で自動更新される
- 日足比較表が日次で自動生成される
- 参照プロセスから比較表を任意タイミングで閲覧できる

## 2025-07-19 02:11 JST [assistant]
- ScheduleRange の条件を用いて DailyComparison の日足を計算する方法を明文化してください。具体的には `Window().TimeFrame<TSchedule>()` でスケジュールテーブルを参照し、IF 条件で `openTime <= RateTimestamp < closeTime` を適用します。
- 上記の処理を応用し、1分足、5分足、60分足の集計も同時に生成すること。必要に応じて `ScheduleRange` の範囲を分解して各分足を計算してください。
