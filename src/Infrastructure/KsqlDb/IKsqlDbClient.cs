namespace Kafka.Ksql.Linq.Infrastructure.KsqlDb;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IKsqlDbClient
{
    Task<Kafka.Ksql.Linq.KsqlDbResponse> ExecuteStatementAsync(string statement);
    Task<Kafka.Ksql.Linq.KsqlDbResponse> ExecuteExplainAsync(string ksql);
    /// <summary>
    /// Executes SHOW TABLES and returns the Kafka topic names of all tables.
    /// </summary>
    Task<HashSet<string>> GetTableTopicsAsync();
}
