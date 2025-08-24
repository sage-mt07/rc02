# 差分履歴: JoinQueryGenerator

🗕 2025-08-02
🧐 作業者: assistant

## Composite key join support
- Retyped key selector lambdas in `JoinQueryGenerator.BuildJoinExpression` to allow anonymous composite keys.

## 変更理由
- Failing unit test for composite key joins due to parameter type mismatch.

## 追加・修正内容（反映先: oss_design_combined.md）
- `BuildJoinExpression` rewraps lambdas with `Func<T, TKey>` signatures before calling `Queryable.Join`.

## 参考文書
- tests/Query/Pipeline/JoinQueryGeneratorTests.cs
