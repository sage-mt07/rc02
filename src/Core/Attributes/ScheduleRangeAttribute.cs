using System;

namespace Kafka.Ksql.Linq.Core.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class ScheduleRangeAttribute : Attribute
{
    public string OpenPropertyName { get; }
    public string ClosePropertyName { get; }

    public ScheduleRangeAttribute() : this("Open", "Close")
    {
    }

    public ScheduleRangeAttribute(string openPropertyName, string closePropertyName)
    {
        OpenPropertyName = openPropertyName;
        ClosePropertyName = closePropertyName;
    }
}
