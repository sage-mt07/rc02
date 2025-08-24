using System.Linq.Expressions;

namespace Kafka.Ksql.Linq.Query.Builders;
internal class GroupByKeyCountVisitor : ExpressionVisitor
{
    public int KeyCount { get; private set; }

    protected override Expression VisitNew(NewExpression node)
    {
        KeyCount += node.Arguments.Count;
        return base.VisitNew(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // NewExpression内でない単独のMemberは1つのキー
        if (KeyCount == 0)
        {
            KeyCount = 1;
        }
        return base.VisitMember(node);
    }
}
