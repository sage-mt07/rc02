using System;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Pipeline;

public class WindowValidatorTests
{
    [Fact]
    public void Validate_Throws_When_BaseUnit_Missing()
    {
        var res = new ExpressionAnalysisResult();
        res.Windows.Add("5s");
        var ex = Assert.Throws<InvalidOperationException>(() => WindowValidator.Validate(res));
        Assert.Equal("Base unit is required for tumbling windows.", ex.Message);
    }

    [Fact]
    public void Validate_Throws_When_BaseUnit_Not_Divide_60()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 7 };
        res.Windows.Add("7s");
        var ex = Assert.Throws<InvalidOperationException>(() => WindowValidator.Validate(res));
        Assert.Equal("Base unit must divide 60 seconds.", ex.Message);
    }

    [Fact]
    public void Validate_Throws_When_Window_Not_Multiple_Of_Base()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("7s");
        var ex = Assert.Throws<InvalidOperationException>(() => WindowValidator.Validate(res));
        Assert.Equal("Window 7s must be a multiple of base 5s.", ex.Message);
    }

    [Fact]
    public void Validate_Throws_When_Window_Not_Whole_Minutes()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("70s");
        var ex = Assert.Throws<InvalidOperationException>(() => WindowValidator.Validate(res));
        Assert.Equal("Windows ≥ 1 minute must be whole-minute multiples.", ex.Message);
    }

    [Fact]
    public void Validate_Passes_For_Aligned_Windows()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("10s");
        WindowValidator.Validate(res);
    }

    [Fact]
    public void Validate_Passes_For_Minute_Multiple_Window()
    {
        var res = new ExpressionAnalysisResult { BaseUnitSeconds = 5 };
        res.Windows.Add("120s");
        WindowValidator.Validate(res);
    }
}
