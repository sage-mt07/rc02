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

        var entityPropMap = entityProps.ToDictionary(p => p.Name);

        var projectionProps = ExtractProjectionProperties(model.SelectProjection, resultType)
            .Where(p => entityPropMap.ContainsKey(p.Name))
            .ToArray();

        if (entityProps.Length != projectionProps.Length)
            throw new InvalidOperationException("Select projection does not match POCO properties.");

        for (int i = 0; i < entityProps.Length; i++)
        {
            if (entityProps[i].Name != projectionProps[i].Name)
                throw new InvalidOperationException("Select projection does not match POCO property order.");
            if (entityProps[i].PropertyType != projectionProps[i].PropertyType)
                throw new InvalidOperationException("Select projection property types do not match POCO.");
        }

        var entityKeys = entityProps
            .Select(p => (Prop: p, Attr: p.GetCustomAttribute<KsqlKeyAttribute>(true)))
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Prop.Name)
            .ToArray();

        var projectionKeys = projectionProps
            .Select(p => (Name: p.Name, Attr: entityPropMap.TryGetValue(p.Name, out var ep)
                ? ep.GetCustomAttribute<KsqlKeyAttribute>(true)
                : null))
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Name)
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
                    if (resultType.GetProperty(mem.Name) != null)
                        props.Add(mem);
                }
                break;
            case MemberInitExpression initExpr:
                foreach (var binding in initExpr.Bindings.OfType<MemberAssignment>())
                {
                    if (resultType.GetProperty(binding.Member.Name) != null)
                        props.Add((PropertyInfo)binding.Member);
                }
                break;
            case ParameterExpression:
                props.AddRange(resultType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .OrderBy(p => p.MetadataToken));
                break;
            case MemberExpression me when me.Member is PropertyInfo pi:
                if (resultType.GetProperty(pi.Name) != null)
                    props.Add(pi);
                break;
        }
        return props;
    }
}
