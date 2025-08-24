using System;

namespace Kafka.Ksql.Linq.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class MaxLengthAttribute : Attribute
{
    public int Length { get; }

    public MaxLengthAttribute(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Max length must be at least 1", nameof(length));

        Length = length;
    }

    public override string ToString()
    {
        return $"MaxLength: {Length}";
    }
}
