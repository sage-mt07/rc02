namespace Kafka.Ksql.Linq.Query.Abstractions;
/// <summary>
/// ビルダー種別列挙
/// </summary>
public enum KsqlBuilderType
{
    Select,
    Where,
    GroupBy,
    Having,
    Join,
    Projection,
    OrderBy
}
