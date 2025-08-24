# 🧪 Query系 DSL 複合パターン テスト指示集

## 1. OrderBy + ThenByDescending パターン

```csharp
context.Orders
    .OrderBy(o => o.CustomerId)
    .ThenByDescending(o => o.OrderDate)
    .Select(o => o.Id);
```

🧭 対象処理範囲：OrderByClauseBuilder の分岐と KsqlExpressionVisitor の対応確認


## 2. Where + Join 条件付きパターン

```csharp
context.Orders
    .Where(o => o.Amount > 1000)
    .Join(context.Customers)
    .On((o, c) => o.CustomerId == c.Id)
    .Select((o, c) => new { o.Id, c.Name });
```

🧭 対象処理範囲：JoinBuilder と WhereClauseBuilder の同時動作確認


## 3. 匿名型 Select（2項目）

```csharp
context.Orders
    .Select(o => new { o.Id, o.Amount });
```

🧭 対象処理範囲：SelectClauseBuilder での匿名型メンバー展開確認


## 4. 匿名型 Select（3項目＋型変換）

```csharp
context.Orders
    .Select(o => new { o.Id, Date = o.OrderDate.Date, o.Amount });
```

🧭 対象処理範囲：型変換と複数プロパティの展開に対応できるか確認


## 5. Window + GroupBy の複合

```csharp
context.Clicks
    .Window(TimeSpan.FromMinutes(5))
    .GroupBy(x => x.UserId)
    .Select(g => new { g.Key, Count = g.Count() });
```

🧭 対象処理範囲：WindowBuilder と GroupByClauseBuilder の連携確認


## 6. Join（匿名型 equals）構文

```csharp
context.Orders
    .Join(context.Payments)
    .On(o => new { o.Id } equals p => new { p.OrderId })
    .Select((o, p) => new { o.Id, p.Status });
```

🧭 対象処理範囲：JoinBuilder の匿名型 equals サポート


## 7. 複合キー GroupBy

```csharp
context.Orders
    .GroupBy(o => new { o.CustomerId, o.ProductId })
    .Select(g => new { g.Key.CustomerId, g.Key.ProductId, Count = g.Count() });
```

🧭 対象処理範囲：GroupByClauseBuilder での匿名型キー解析


## 8. Where + Select 条件付き抽出

```csharp
context.Orders
    .Where(o => o.Amount > 1000)
    .Select(o => o.Id);
```

🧭 対象処理範囲：WhereClauseBuilder + SelectClauseBuilder の連携


## 9. Window + OrderBy の組み合わせ

```csharp
context.Logs
    .Window(TimeSpan.FromMinutes(1))
    .OrderBy(l => l.Timestamp)
    .Select(l => l.Message);
```

🧭 対象処理範囲：WindowBuilder と OrderByClauseBuilder の動作確認


## 10. GroupBy + Having 条件あり

```csharp
context.Orders
    .GroupBy(o => o.CustomerId)
    .Having(g => g.Count() > 5)
    .Select(g => new { g.Key, Count = g.Count() });
```

🧭 対象処理範囲：GroupByClauseBuilder + HavingClauseBuilder の対応確認

