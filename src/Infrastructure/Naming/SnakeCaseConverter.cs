namespace Kafka.Ksql.Linq.Infrastructure.Naming;

using System.Text;
using System.Text.RegularExpressions;

internal static class SnakeCaseConverter
{
    public static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var sb = new StringBuilder();
        for (int i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
