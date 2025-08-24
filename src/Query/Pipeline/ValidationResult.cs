using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// 検証結果
/// </summary>
internal record ValidationResult(bool IsValid, List<string> Errors)
{
    /// <summary>
    /// 成功結果
    /// </summary>
    public static ValidationResult Success => new(true, new List<string>());

    /// <summary>
    /// 失敗結果作成
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult(false, errors.ToList());
    }

    /// <summary>
    /// エラーメッセージ結合
    /// </summary>
    public string GetErrorMessage()
    {
        return Errors.Count > 0 ? string.Join("; ", Errors) : string.Empty;
    }
}
