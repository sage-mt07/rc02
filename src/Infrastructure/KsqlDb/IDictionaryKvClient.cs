namespace Kafka.Ksql.Linq.Infrastructure.KsqlDb;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDictionaryKvClient
{
    Task<string?> GetAsync(string key);
    Task<Dictionary<string, string>> GetByPrefixAsync(string prefix);
    Task UpsertAsync(string key, string value);
}
