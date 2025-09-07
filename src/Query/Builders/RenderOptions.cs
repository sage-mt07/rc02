namespace Kafka.Ksql.Linq.Query.Builders;

public enum KeyPathStyle
{
    None,
    Dot,
    Arrow
}

public class RenderOptions
{
    public KeyPathStyle KeyPathStyle { get; set; } = KeyPathStyle.None;
}
