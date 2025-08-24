using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Messaging;

namespace Kafka.Ksql.Linq.Core.Abstractions;

/// <summary>
/// クエリ・更新共通操作の統一インターフェース
/// LINQ互換性を維持
/// </summary>
public interface IEntitySet<T> : IAsyncEnumerable<T> where T : class
{
    // Producer operations
    Task AddAsync(T entity, Dictionary<string, string>? headers = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(T entity, CancellationToken cancellationToken = default);

    // Consumer operations
    Task<List<T>> ToListAsync(CancellationToken cancellationToken = default);

    // Streaming operations
    Task ForEachAsync(Func<T, Task> action, TimeSpan timeout = default, bool autoCommit = true, CancellationToken cancellationToken = default);

    [Obsolete("Use ForEachAsync(Func<T, Dictionary<string,string>, MessageMeta, Task>)")]
    Task ForEachAsync(Func<T, Dictionary<string,string>, Task> action, TimeSpan timeout = default, bool autoCommit = true, CancellationToken cancellationToken = default);

    Task ForEachAsync(Func<T, Dictionary<string,string>, MessageMeta, Task> action, TimeSpan timeout = default, bool autoCommit = true, CancellationToken cancellationToken = default);



    // Metadata
    string GetTopicName();
    EntityModel GetEntityModel();
    IKsqlContext GetContext();
}
