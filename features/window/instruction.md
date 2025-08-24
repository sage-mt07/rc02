To naruse
Window関数で足を作れるけど、足のclose timeは機械的に決まらないため、この機能を入れる。
以下の要件でまず設計を行ってください。
・Window().TimeFrame<marketschedulepoco>()のIFとする
marketschedulepocoには複数のPKを持つことを想定する
marketschedulepocoにはopen/closeの日時を持つプロパティが存在する
区間の境界値は「左閉右開」[Open, Close)
5分足等、1分足以上の場合、このOpen/Closeの範囲でしか足を作成しない
設計内容には既存のドキュメント修正を含みます

### 追加要件
- Window 処理は `[ScheduleOpen]` ～ `[ScheduleClose]` の範囲に含まれるデータのみを
 取り込み、範囲外のレコードでは足を生成しない。
- 日足生成時に `ScheduleClose` が 6:30 の場合、6:30 未満のデータをその日の終値
  (OHLC の C) として扱い、6:30 以降のレコードは次の日の足に回す。
- 内部では既存 `WindowProcessor`/`WindowFinalizationManager` を利用し、スケジュール
  区間から算出したウィンドウ境界を渡す方式とする。
- 足別トピックは Table として作成されるため RocksDB ベースのキャッシュが既定で有効
  となる。この挙動をテスト・ドキュメントで確認すること。
