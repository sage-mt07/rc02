using Kafka.Ksql.Linq.Core.Models;
using System;

namespace Kafka.Ksql.Linq.Query.Schema;

/// <summary>
/// クエリによって生成されるKey/Value構造の定義
/// </summary>
public class KeyValueSchemaInfo
{
    public string ClassName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public PropertyMeta[] Properties { get; set; } = Array.Empty<PropertyMeta>();
    public string SchemaVersion { get; set; } = "1";
    public string Compatibility { get; set; } = string.Empty;
}
