using Kafka.Ksql.Linq.Core.Attributes;
using System;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Core;

public class DefaultAndMaxLengthAttributeTests
{

    [Fact]
    public void MaxLengthAttribute_StoresLength()
    {
        var attr = new MaxLengthAttribute(5);
        Assert.Equal(5, attr.Length);
        Assert.Equal("MaxLength: 5", attr.ToString());
    }

    [Fact]
    public void MaxLengthAttribute_InvalidLength_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MaxLengthAttribute(0));
    }
}

