using System;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Pipeline;

public class WindowValidatorTests
{
    [Fact]
    public void Validate_Throws_When_Window_Not_Multiple()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("7s");
        var ex = Assert.Throws<InvalidOperationException>(() => WindowValidator.Validate(res));
        Assert.Equal("Window 7s must be a multiple of base 5s.", ex.Message);
    }

    [Fact]
    public void Validate_Passes_For_Aligned_Windows()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("10s");
        WindowValidator.Validate(res);
    }
}
