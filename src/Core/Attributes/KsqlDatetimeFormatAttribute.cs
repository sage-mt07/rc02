using System;

namespace Kafka.Ksql.Linq.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class KsqlDatetimeFormatAttribute : Attribute
{
    public string Format { get; }

    public KsqlDatetimeFormatAttribute(string format)
    {
        Format = format;
    }
}
