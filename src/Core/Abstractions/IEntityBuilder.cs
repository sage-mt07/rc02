namespace Kafka.Ksql.Linq.Core.Abstractions;

public interface IEntityBuilder<T> where T : class
{
    IEntityBuilder<T> AsTable(string? topicName = null, bool useCache = true);
    IEntityBuilder<T> AsStream();
}
