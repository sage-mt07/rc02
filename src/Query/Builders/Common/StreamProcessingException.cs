using System;

namespace Kafka.Ksql.Linq.Query.Builders.Common;

/// <summary>
/// ストリーム処理例外
/// </summary>
internal class StreamProcessingException : Exception
{
    public StreamProcessingException(string message) : base(message) { }

    public StreamProcessingException(string message, Exception innerException) : base(message, innerException) { }
}
