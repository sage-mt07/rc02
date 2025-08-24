using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Kafka.Ksql.Linq.Core.Abstractions;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal static class SchemaValidator
{
    private static readonly HashSet<string> _checked = new();

    public static void Reset() => _checked.Clear();

    public static void Validate(IReadOnlyList<QuerySpec> specs, IReadOnlyList<EntityModel> models)
    {
        foreach (var spec in specs)
        {
            var model = models.First(m => (string)m.AdditionalSettings["id"] == spec.TargetId);
            if (!model.AdditionalSettings.TryGetValue("role", out var r) || (string)r == "Hb")
                continue;
            var projection = (string[])model.AdditionalSettings["projection"];
            var types = model.AdditionalSettings.TryGetValue("projection/types", out var tObj) && tObj is Type[] tArr
                ? tArr
                : Array.Empty<Type>();
            var nulls = model.AdditionalSettings.TryGetValue("projection/nulls", out var nObj) && nObj is bool[] nArr
                ? nArr
                : Array.Empty<bool>();
            if (types.Length != projection.Length || nulls.Length != projection.Length)
                throw new InvalidOperationException("projection metadata length mismatch");
            var hash = ComputeHash(projection, types, nulls);
            if (_checked.Add(hash))
            {
                if (!projection.SequenceEqual(spec.ColumnPlan))
                {
                    var len = Math.Max(projection.Length, spec.ColumnPlan.Length);
                    for (int i = 0; i < len; i++)
                    {
                        var exp = i < projection.Length ? projection[i] : "(missing)";
                        var act = i < spec.ColumnPlan.Length ? spec.ColumnPlan[i] : "(missing)";
                        if (exp != act)
                        {
                            var type = types[i].Name;
                            var nullable = nulls[i] ? "?" : string.Empty;
                            throw new InvalidOperationException($"index:{i} expected:{exp}({type}{nullable}) actual:{act}");
                        }
                    }
                }
            }
        }
    }

    private static string ComputeHash(string[] cols, Type[] types, bool[] nulls)
    {
        using var sha = SHA256.Create();
        var parts = cols.Select((c, i) => $"{c}:{types[i].FullName}:{(nulls[i] ? 1 : 0)}");
        var bytes = Encoding.UTF8.GetBytes(string.Join("|", parts));
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }
}
