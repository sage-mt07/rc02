# 🧪 QueryBuilder 複合テスト指示書

## 📌 目的

`Kafka.Ksql.Linq.Query` 系のクエリビルダー（Where, Join, Select, GroupBy, Window 等）に対し、**複合パターンの網羅テスト**を実施し、コードカバレッジとブランチ網羅率を向上させることを目的とします。

---

## ✅ 指示内容一覧

### ❶ Where + Join の複合パターン

- **目的**: Join 条件付き Where フィルタの解析動作を確認
- **例**:

```csharp
context.Orders
    .Where(o => o.Amount > 1000)
    .Join(context.Customers)
    .On((o, c) => o.CustomerId == c.Id)
    .Select((o, c) => new { o.Id, c.Name });
```

### ❷ GroupBy + Window の複合パターン

- **目的**: ウィンドウごとのグループ化＋集計の動作確認
- **例**:

```csharp
context.Clicks
    .Window(TimeSpan.FromMinutes(5))
    .GroupBy(x => x.UserId)
    .Select(g => new { g.Key, Count = g.Count() });
```

### ❸ Join + 匿名型条件（新構文）

- **目的**: KSQL風の `equals` 条件が正常にパースされるか確認
- **例**:

```csharp
context.Orders
    .Join(context.Payments)
    .On(o => new { o.Id } equals p => new { p.OrderId })
    .Select((o, p) => new { o.Id, p.Status });
```

### ❹ OrderBy + ThenByDescending

- **目的**: 複数カラムでの並び替え（昇順＋降順）の組み合わせ確認
- **例**:

```csharp
context.Orders
    .OrderBy(o => o.CustomerId)
    .ThenByDescending(o => o.OrderDate)
    .Select(o => o.Id);
```

### ❺ Select + 匿名型 or Tuple

- **目的**: 匿名型のプロパティ組み合わせの展開確認
- **例**:

```csharp
context.Orders
    .Select(o => new { o.Id, o.Amount, o.OrderDate });
```

---

## 📂 配置場所

- 保存先: `tests/Query/QueryBuilderIntegrationTests.cs`
- フォーマット: `xUnit`、`[Theory]` + `[InlineData]` 可

---

## 🎯 期待効果

- 全体行カバレッジ +3%
- ブランチ網羅率 +4%
- Query 系 DSL の組み合わせテスト基盤の確立

---

## 🧠 備考

複合的なメソッドチェーンの式解析に失敗した場合は、`KsqlQueryBuilder` の式分解ロジックを別途ユニットテストで補完してください。
