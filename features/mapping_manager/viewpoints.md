# MappingManager API Specification and Test Viewpoints

本ドキュメントでは `MappingManager` の公開 API とくに `ExtractKeyValue<TEntity>()` の挙動を詳細化し、テスト観点を整理する。

## 1. API 詳細

### Register<TEntity>(EntityModel model)
- **目的**: エンティティ型と `EntityModel` の対応を登録する。
- **失敗条件**:
  - `model` が `null` の場合 `ArgumentNullException`。
  - 同一エンティティ型を再登録した場合は上書きされる。

### ExtractKeyValue<TEntity>(TEntity entity)
- **目的**: 登録済みモデルを用いてエンティティからキーと値を取り出す。
- **処理手順**:
  1. `entity` が `null` の場合 `ArgumentNullException` を送出。
  2. 登録済みモデルが存在しない場合 `InvalidOperationException` を送出。
  3. `KeyExtractor.ExtractKeyValue` を呼び出し、抽出したキーとエンティティを返す。
- **戻り値**: `(object Key, TEntity Value)`

## 2. テストケース一覧

### 正常系
1. **単一キーが登録されている場合、キー値とエンティティが返る**
2. **複合キーが登録されている場合、`Dictionary<string, object>` としてキーが返る**
3. **`Register` を複数回呼び出すと後の登録で上書きされる**
4. **キーが登録されていない場合でも `KeyExtractor` が `Guid` を生成し正常に返る**
5. **`int` `long` `string` `Guid` 各型のキー値が `KeyToString` で正しく文字列化される**

### 異常系
1. **未登録のエンティティを渡すと `InvalidOperationException` が発生する**
2. **`ExtractKeyValue` に `null` を渡すと `ArgumentNullException`**
3. **複合キーの一部が `null` の場合でも空文字列として処理される**
4. **キー型が `int` `long` `string` `Guid` 以外の場合は `IsSupportedKeyType` が `false`**

## 3. KeyExtractor ロジック確認ポイント
- `IsCompositeKey` は `KeyProperties.Length > 1` かどうかで判定する。
- 未設定時は `Guid.NewGuid()` を返すため一貫したランダムキーとなる。
- 複合キーはプロパティ名→値の辞書に変換し、`null` は空文字列へ変換される。
- `KeyToString` で `Dictionary` 型を "`key=value`" 形式で連結している。
- `IsSupportedKeyType` では `Nullable<T>` の実体型を評価する。
- 複合キー辞書のプロパティ順序がモデル定義順で保持されるか確認する。

## 4. 現状仕様で不足する論点
- モデル再登録のロック機構がなく、マルチスレッド環境で競合する可能性。
- `ExtractKeyValue` が内部で `KeyExtractor` を固定呼び出ししており、拡張性が限定的。
- 複合キー辞書の順序保証が明文化されていない。

## 5. 運用上の落とし穴
- `MappingManager` を毎回 `new` すると登録漏れが発生しやすい。
- Key 型制約 (`int`,`long`,`string`,`Guid`) を逸脱するとシリアライズ時に失敗する。
- `EntityModel` でキー未定義のまま登録するとテストで意図しない `Guid` が生成される。


## 6. API/例外設計チェック結果
- `Register<TEntity>` は `null` 引数に `ArgumentNullException` を送出し、同一型は上書き登録される。
- `ExtractKeyValue` は未登録モデルに `InvalidOperationException`、`null` 引数に `ArgumentNullException` を送出する。
- 抽出したキーが `Dictionary<string, object>` 以外で `IsSupportedKeyType` が `false` の場合 `NotSupportedException`。
- `KeyExtractor` からの `ArgumentException` `InvalidCastException` `FormatException` は `InvalidOperationException` にラップされる。
- 複合キーは定義順を維持した `Dictionary` として返却され、`null` 値は空文字列に変換される。

## 7. テスト実行概要 (2025-07-13 JST)
- `MappingManagerTests` と `KeyExtractorTests` を実行し、複合キー処理と型変換ロジックを確認。
- 結果: **Passed 591 / Failed 0 / Skipped 10**。

