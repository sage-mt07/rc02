using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Builders.Common;
using Kafka.Ksql.Linq.Query.Builders.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kafka.Ksql.Linq.Query.Builders;
/// <summary>
/// ExpressionVisitor specialized for SELECT clause
/// </summary>
internal class SelectExpressionVisitor : ExpressionVisitor
{
    private readonly List<string> _columns = new();
    private readonly HashSet<string> _usedAliases = new();
    private readonly HashSet<string> _selectedGroupKeys = new();
    private readonly System.Collections.Generic.IDictionary<string, string>? _paramToSource;
    private readonly System.Collections.Generic.ISet<string>? _excludeAliases;

    public SelectExpressionVisitor() { }
    public SelectExpressionVisitor(System.Collections.Generic.IDictionary<string, string> paramToSource)
    {
        _paramToSource = paramToSource;
    }
    public SelectExpressionVisitor(System.Collections.Generic.IDictionary<string, string> paramToSource, System.Collections.Generic.ISet<string> excludeAliases)
    {
        _paramToSource = paramToSource;
        _excludeAliases = new HashSet<string>(excludeAliases);
    }

    public string GetResult()
    {
        return _columns.Count > 0 ? string.Join(", ", _columns) : string.Empty;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        // When used with object initializers, the constructor is parameterless
        // and does not map DTO members. In such cases validation should occur
        // in VisitMemberInit instead of here to avoid false mismatches.
        if (node.Members is { Count: > 0 } || node.Arguments.Count > 0)
        {
            ValidateGroupKeyDtoOrder(node);
        }
        // 匿名型の射影処理
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            var arg = node.Arguments[i];
            var memberName = node.Members?[i]?.Name ?? $"col{i}";

            if (IsGroupKeyObject(arg))
            {
                AddGroupKeyColumns();
            }
            else
            {
                var columnExpression = ProcessProjectionArgument(arg);
                var alias = GenerateUniqueAlias(memberName);
                if (!string.Equals(columnExpression, alias, StringComparison.OrdinalIgnoreCase))
                    _columns.Add($"{columnExpression} AS {alias}");
                else
                    _columns.Add(columnExpression);
            }
        }

        return node;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        ValidateGroupKeyDtoOrder(node);
        foreach (var binding in node.Bindings.OfType<MemberAssignment>())
        {
            var memberName = binding.Member.Name;
            var expr = binding.Expression;
            if (IsGroupKeyObject(expr))
            {
                AddGroupKeyColumns();
            }
            else
            {
                var columnExpression = ProcessProjectionArgument(expr);
                var alias = GenerateUniqueAlias(memberName);
                var needsAlias = !string.Equals(columnExpression, alias, StringComparison.OrdinalIgnoreCase)
                                || IsGroupKeyMember(expr);
                if (needsAlias)
                    _columns.Add($"{columnExpression} AS {alias}");
                else
                    _columns.Add(columnExpression);
            }
        }
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (IsGroupKeyObject(node))
        {
            AddGroupKeyColumns();
        }
        else
        {
            // 単純なプロパティアクセス
            var columnName = GetColumnName(node);
            _columns.Add(columnName);
        }
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        // SELECT * の場合
        _columns.Add("*");
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // 関数呼び出しの処理
        var functionCall = KsqlFunctionTranslator.TranslateMethodCall(node);
        _columns.Add(functionCall);
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // 計算式の処理
        var left = ProcessExpression(node.Left);
        var right = ProcessExpression(node.Right);
        var varoperator = GetOperator(node.NodeType);

        var expression = $"({left} {varoperator} {right})";
        _columns.Add(expression);
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        // CASE式の処理
        BuilderValidation.ValidateConditionalTypes(node.IfTrue, node.IfFalse);
        var test = ProcessExpression(node.Test);
        var ifTrue = ProcessExpression(node.IfTrue);
        var ifFalse = ProcessExpression(node.IfFalse);

        var caseExpression = $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
        _columns.Add(caseExpression);
        return node;
    }

    /// <summary>
    /// 射影引数処理
    /// </summary>
    private string ProcessProjectionArgument(Expression arg)
    {
        return arg switch
        {
            MemberExpression member => GetColumnName(member),
            MethodCallExpression methodCall => KsqlFunctionTranslator.TranslateMethodCall(methodCall),
            BinaryExpression binary => ProcessBinaryExpression(binary),
            ConstantExpression constant => SafeToString(constant.Value),
            ConditionalExpression conditional => ProcessConditionalExpression(conditional),
            UnaryExpression unary => ProcessExpression(unary.Operand),
            _ => ProcessExpression(arg)
        };
    }

    /// <summary>
    /// 二項式処理
    /// </summary>
    private string ProcessBinaryExpression(BinaryExpression binary)
    {
        var left = ProcessExpression(binary.Left);
        var right = ProcessExpression(binary.Right);
        var varoperator = GetOperator(binary.NodeType);
        return $"({left} {varoperator} {right})";
    }

    /// <summary>
    /// 条件式処理
    /// </summary>
    private string ProcessConditionalExpression(ConditionalExpression conditional)
    {
        var test = ProcessExpression(conditional.Test);
        var ifTrue = ProcessExpression(conditional.IfTrue);
        var ifFalse = ProcessExpression(conditional.IfFalse);
        return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
    }

    /// <summary>
    /// 汎用式処理
    /// </summary>
    private string ProcessExpression(Expression expression)
    {
        return expression switch
        {
            MemberExpression member => GetColumnName(member),
            ConstantExpression constant => SafeToString(constant.Value),
            MethodCallExpression methodCall => KsqlFunctionTranslator.TranslateMethodCall(methodCall),
            BinaryExpression binary => ProcessBinaryExpression(binary),
            UnaryExpression unary => ProcessExpression(unary.Operand),
            _ => expression.ToString()
        };
    }

    /// <summary>
    /// カラム名取得
    /// </summary>
    private string GetColumnName(MemberExpression member)
    {
        // プロパティアクセスチェーンを分解
        var stack = new System.Collections.Generic.Stack<string>();
        Expression? expr = member;
        while (expr is MemberExpression me)
        {
            stack.Push(me.Member.Name);
            expr = me.Expression;
        }

        if (expr is not ParameterExpression pe)
            throw new InvalidOperationException("Unqualified column access is not allowed. Use source parameter properties.");

        var path = stack.ToArray();

        // g.Key.X パターンの処理
        if (path.Length > 0 && path[0] == "Key")
        {
            var prefix = GetKeyPrefix(pe);
            var cols = path[1..];
            for (int i = 0; i < cols.Length; i++)
                cols[i] = KsqlNameUtils.Sanitize(cols[i]).ToUpperInvariant();
            var name = string.Join('.', cols);
            return $"{prefix}.{name}";
        }

        if (member.Member is PropertyInfo prop && prop.GetCustomAttribute<KsqlKeyAttribute>() != null)
        {
            var prefix = GetKeyPrefix(pe);
            var name = KsqlNameUtils.Sanitize(prop.Name).ToUpperInvariant();
            return $"{prefix}.{name}";
        }

        // 通常プロパティアクセス
        for (int i = 0; i < path.Length; i++)
            path[i] = KsqlNameUtils.Sanitize(path[i]);

        var col = string.Join(".", path);
        if (_paramToSource != null && _paramToSource.TryGetValue(pe.Name ?? string.Empty, out var source))
            return $"{source}.{col}";
        return col;
    }

    /// <summary>
    /// 一意エイリアス生成
    /// </summary>
    private string GenerateUniqueAlias(string baseName)
    {
        var alias = baseName;
        var counter = 1;

        while (_usedAliases.Contains(alias))
        {
            alias = $"{baseName}_{counter}";
            counter++;
        }

        _usedAliases.Add(alias);
        return alias;
    }

    /// <summary>
    /// 演算子変換
    /// </summary>
    private static string GetOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            ExpressionType.Modulo => "%",
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "<>",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported in SELECT clause")
        };
    }

    /// <summary>
    /// NULL-safe string conversion
    /// </summary>
    private static string SafeToString(object? value)
    {
        return BuilderValidation.SafeToString(value);
    }

    private static bool IsGroupKeyObject(Expression expr)
    {
        return expr is MemberExpression member &&
               member.Member.Name == "Key" &&
               member.Expression is ParameterExpression;
    }

    private static bool IsGroupKeyMember(Expression expr)
    {
        if (expr is not MemberExpression member)
            return false;

        if (member.Member.Name == "Key" && member.Expression is ParameterExpression)
            return true;

        if (member.Expression is MemberExpression inner &&
            inner.Member.Name == "Key" &&
            inner.Expression is ParameterExpression)
            return true;

        return false;
    }

    private IEnumerable<(string expr, string alias)> GetGroupKeyColumns()
    {
        var keys = GroupByClauseBuilder.LastBuiltKeys;
        if (string.IsNullOrWhiteSpace(keys))
            return Array.Empty<(string, string)>();

        return keys
            .Split(',')
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Select(s =>
            {
                var alias = s.Contains('.') ? s.Split('.').Last() : s;
                return (s, alias);
            });
    }

    private void AddGroupKeyColumns()
    {
        foreach (var (expr, alias) in GetGroupKeyColumns())
        {
            if (_selectedGroupKeys.Contains(alias))
                continue;
            _selectedGroupKeys.Add(alias);
            _columns.Add($"{expr} AS {alias}");
        }
    }

    private string GetKeyPrefix(ParameterExpression pe)
    {
        if (_paramToSource != null && _paramToSource.TryGetValue(pe.Name ?? string.Empty, out var alias))
            return alias;

        var type = GetGroupingElementType(pe.Type);
        return KeyNameResolver.GetKeyPrefix(type).Replace(".key", string.Empty);
    }

    private static Type GetGroupingElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            return type.GetGenericArguments()[1];
        return type;
    }

    private void ValidateGroupKeyDtoOrder(NewExpression node)
    {
        if (node.Type == null)
            return;

        if (IsAnonymousType(node.Type))
            return;

        var groupKeys = GetGroupKeyColumns().Select(x => x.alias).ToList();
        if (groupKeys.Count == 0)
            return;

        var dtoKeyMembers = node.Arguments
            .Zip(node.Members ?? Enumerable.Empty<MemberInfo>(), (arg, mem) => new { arg, mem })
            .Where(x => IsGroupKeyMember(x.arg))
            .Select(x => x.mem.Name)
            .ToList();

        if (dtoKeyMembers.Count != groupKeys.Count || !dtoKeyMembers.SequenceEqual(groupKeys, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The order of GroupBy keys does not match the output DTO definition. Please ensure they are the same order.");
        }
    }

    private void ValidateGroupKeyDtoOrder(MemberInitExpression node)
    {
        if (IsAnonymousType(node.Type))
            return;

        var groupKeys = GetGroupKeyColumns().Select(x => x.alias).ToList();
        if (groupKeys.Count == 0)
            return;

        var dtoKeyMembers = node.Bindings
            .OfType<MemberAssignment>()
            .Where(b => IsGroupKeyMember(b.Expression))
            .Select(b => b.Member.Name)
            .ToList();

        if (dtoKeyMembers.Count != groupKeys.Count || !dtoKeyMembers.SequenceEqual(groupKeys, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The order of GroupBy keys does not match the output DTO definition. Please ensure they are the same order.");
        }
    }

    private static bool IsAnonymousType(Type type)
    {
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) &&
               type.IsGenericType && type.Name.Contains("AnonymousType");
    }
}
