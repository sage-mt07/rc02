using System;
using System.Collections.Generic;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Adapters;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Adapters;

public class SchemaValidatorTests
{
    [Fact]
    public void SchemaHash_Validates_Once_PerPoco_Skips_HB()
    {
        var model = new EntityModel { EntityType = typeof(object) };
        model.AdditionalSettings["id"] = "live1";
        model.AdditionalSettings["projection"] = new[] { "A" };
        model.AdditionalSettings["projection/types"] = new[] { typeof(int) };
        model.AdditionalSettings["projection/nulls"] = new[] { false };
        model.AdditionalSettings["role"] = "Live";

        var specs = new List<QuerySpec>
        {
            new() { TargetId = "live1", ColumnPlan = new[] { "A" } },
            new() { TargetId = "live1", ColumnPlan = new[] { "X" } }
        };
        SchemaValidator.Reset();
        SchemaValidator.Validate(specs, new List<EntityModel> { model });

        var hbModel = new EntityModel { EntityType = typeof(object) };
        hbModel.AdditionalSettings["id"] = "hb1";
        hbModel.AdditionalSettings["projection"] = new[] { "A" };
        hbModel.AdditionalSettings["role"] = "Hb";
        SchemaValidator.Validate(new List<QuerySpec> { new() { TargetId = "hb1", ColumnPlan = new[] { "X" } } }, new List<EntityModel> { hbModel });
    }

    [Fact]
    public void SchemaMismatch_Fails_With_PositionedDiff()
    {
        var model = new EntityModel { EntityType = typeof(object) };
        model.AdditionalSettings["id"] = "live1";
        model.AdditionalSettings["projection"] = new[] { "A", "B" };
        model.AdditionalSettings["projection/types"] = new[] { typeof(int), typeof(string) };
        model.AdditionalSettings["projection/nulls"] = new[] { false, false };
        model.AdditionalSettings["role"] = "Live";
        var spec = new QuerySpec { TargetId = "live1", ColumnPlan = new[] { "A", "X" } };
        SchemaValidator.Reset();
        var ex = Assert.Throws<InvalidOperationException>(() => SchemaValidator.Validate(new List<QuerySpec> { spec }, new List<EntityModel> { model }));
        Assert.Contains("index:1", ex.Message);
        Assert.Contains("expected:B", ex.Message);
        Assert.Contains("actual:X", ex.Message);
    }

    [Fact]
    public void Validator_Skips_HB_Safely_And_Hash_Includes_TypeNullability()
    {
        var m1 = new EntityModel { EntityType = typeof(object) };
        m1.AdditionalSettings["id"] = "live1";
        m1.AdditionalSettings["projection"] = new[] { "A" };
        m1.AdditionalSettings["projection/types"] = new[] { typeof(int) };
        m1.AdditionalSettings["projection/nulls"] = new[] { false };
        m1.AdditionalSettings["role"] = "Live";

        var m2 = new EntityModel { EntityType = typeof(object) };
        m2.AdditionalSettings["id"] = "live1";
        m2.AdditionalSettings["projection"] = new[] { "A" };
        m2.AdditionalSettings["projection/types"] = new[] { typeof(int) };
        m2.AdditionalSettings["projection/nulls"] = new[] { true };
        m2.AdditionalSettings["role"] = "Live";

        var spec1 = new QuerySpec { TargetId = "live1", ColumnPlan = new[] { "A" } };
        var spec2 = new QuerySpec { TargetId = "live1", ColumnPlan = new[] { "X" } };
        SchemaValidator.Reset();
        SchemaValidator.Validate(new List<QuerySpec> { spec1 }, new List<EntityModel> { m1 });
        Assert.Throws<InvalidOperationException>(() =>
            SchemaValidator.Validate(new List<QuerySpec> { spec2 }, new List<EntityModel> { m2 }));

        var hbModel = new EntityModel { EntityType = typeof(object) };
        hbModel.AdditionalSettings["id"] = "hb1";
        hbModel.AdditionalSettings["projection"] = new[] { "A" };
        var hbSpec = new QuerySpec { TargetId = "hb1", ColumnPlan = new[] { "A" } };
        SchemaValidator.Validate(new List<QuerySpec> { hbSpec }, new List<EntityModel> { hbModel });
    }

    [Fact]
    public void Validator_Throws_On_TypeArray_Length_Mismatch()
    {
        var model = new EntityModel { EntityType = typeof(object) };
        model.AdditionalSettings["id"] = "live1";
        model.AdditionalSettings["projection"] = new[] { "A" };
        model.AdditionalSettings["projection/types"] = Array.Empty<Type>();
        model.AdditionalSettings["role"] = "Live";
        var spec = new QuerySpec { TargetId = "live1", ColumnPlan = new[] { "A" } };
        SchemaValidator.Reset();
        Assert.Throws<InvalidOperationException>(() =>
            SchemaValidator.Validate(new List<QuerySpec> { spec }, new List<EntityModel> { model }));
    }
}
