using System;

namespace Kafka.Ksql.Linq.Query.Builders.Functions;

/// <summary>
/// KSQL関数マッピング定義
/// 設計理由：C#メソッド → KSQL関数の変換規則を定義
/// </summary>
internal record KsqlFunctionMapping(
    string KsqlFunction,
    int MinArgs,
    int MaxArgs = int.MaxValue,
    bool RequiresSpecialHandling = false,
    string? CustomTemplate = null)
{
    /// <summary>
    /// 固定引数数のコンストラクター
    /// </summary>
    public KsqlFunctionMapping(string ksqlFunction, int exactArgs)
        : this(ksqlFunction, exactArgs, exactArgs)
    {
    }

    /// <summary>
    /// カスタムテンプレート付きコンストラクター
    /// </summary>
    public KsqlFunctionMapping(string ksqlFunction, int minArgs, int maxArgs, string customTemplate)
        : this(ksqlFunction, minArgs, maxArgs, false, customTemplate)
    {
    }
    /// <summary>
    /// カスタムテンプレート付きコンストラクター（固定引数数）
    /// </summary>
    public KsqlFunctionMapping(string ksqlFunction, int exactArgs, string customTemplate)
        : this(ksqlFunction, exactArgs, exactArgs, false, customTemplate)
    {
    }

    /// <summary>
    /// 特殊処理フラグ付きコンストラクター（固定引数数）
    /// </summary>
    public KsqlFunctionMapping(string ksqlFunction, int exactArgs, bool requiresSpecialHandling)
        : this(ksqlFunction, exactArgs, exactArgs, requiresSpecialHandling)
    {
    }

    /// <summary>
    /// 特殊処理フラグ + カスタムテンプレート付きコンストラクター（固定引数数）
    /// </summary>
    public KsqlFunctionMapping(string ksqlFunction, int exactArgs, bool requiresSpecialHandling, string? customTemplate)
        : this(ksqlFunction, exactArgs, exactArgs, requiresSpecialHandling, customTemplate)
    {
    }
    /// <summary>
    /// 引数数バリデーション
    /// </summary>
    public bool IsValidArgCount(int argCount)
    {
        return argCount >= MinArgs && argCount <= MaxArgs;
    }

    /// <summary>
    /// テンプレート適用可能性チェック
    /// </summary>
    public bool HasCustomTemplate => !string.IsNullOrEmpty(CustomTemplate);

    /// <summary>
    /// 標準的な関数呼び出し形式生成
    /// </summary>
    public string GenerateStandardCall(params string[] args)
    {
        if (!IsValidArgCount(args.Length))
        {
            throw new ArgumentException($"Invalid argument count for {KsqlFunction}. Expected {MinArgs}-{MaxArgs}, got {args.Length}");
        }

        if (HasCustomTemplate)
        {
            return ApplyCustomTemplate(args);
        }

        return $"{KsqlFunction}({string.Join(", ", args)})";
    }

    /// <summary>
    /// カスタムテンプレート適用
    /// </summary>
    private string ApplyCustomTemplate(string[] args)
    {
        var result = CustomTemplate!;
        for (int i = 0; i < args.Length; i++)
        {
            result = result.Replace($"{{{i}}}", args[i]);
        }
        return result;
    }
}
