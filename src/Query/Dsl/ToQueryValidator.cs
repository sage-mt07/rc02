using Kafka.Ksql.Linq.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kafka.Ksql.Linq.Query.Dsl;

internal static class ToQueryValidator
{
    public static void ValidateSelectMatchesPoco(Type resultType, KsqlQueryModel model)
    {
        if (resultType == null) throw new ArgumentNullException(nameof(resultType));
        if (model == null) throw new ArgumentNullException(nameof(model));

        var entityProps = resultType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.MetadataToken)
            .Where(p => !Attribute.IsDefined(p, typeof(KsqlIgnoreAttribute), true))
            .ToArray();

        var projectionProps = ExtractProjectionProperties(model.SelectProjection, resultType)
            .Where(p => !Attribute.IsDefined(p, typeof(KsqlIgnoreAttribute), true))
            .ToArray();

        if (entityProps.Length != projectionProps.Length)
            throw new InvalidOperationException("Select projection does not match POCO properties.");

        for (int i = 0; i < entityProps.Length; i++)
        {
            if (entityProps[i].Name != projectionProps[i].Name)
                throw new InvalidOperationException("Select projection does not match POCO property order.");
        }

        var entityKeys = entityProps
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<KsqlKeyAttribute>(true)))
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Prop.Name)
            .ToArray();

        var projectionKeys = projectionProps
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<KsqlKeyAttribute>(true)))
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Prop.Name)
            .ToArray();

        if (!entityKeys.SequenceEqual(projectionKeys))
            throw new InvalidOperationException("Select projection key order does not match POCO.");
    }

    private static List<PropertyInfo> ExtractProjectionProperties(LambdaExpression? projection, Type resultType)
    {
        if (projection == null)
            return resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(p => p.MetadataToken)
                .ToList();

        var props = new List<PropertyInfo>();
        switch (projection.Body)
        {
            case NewExpression newExpr when newExpr.Members != null:
                foreach (var mem in newExpr.Members.OfType<PropertyInfo>())
                {
                    var p = resultType.GetProperty(mem.Name);
                    if (p != null) props.Add(p);
                }
                break;
            case MemberInitExpression initExpr:
                foreach (var binding in initExpr.Bindings.OfType<MemberAssignment>())
                {
                    var p = resultType.GetProperty(binding.Member.Name);
                    if (p != null) props.Add(p);
                }
                break;
            case ParameterExpression:
                props.AddRange(resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(p => p.MetadataToken));
                break;
            case MemberExpression me when me.Member is PropertyInfo pi:
                var prop = resultType.GetProperty(pi.Name);
                if (prop != null) props.Add(prop);
                break;
        }
        return props;
    }
}
