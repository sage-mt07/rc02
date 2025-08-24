using System;

namespace Kafka.Ksql.Linq.Core.Abstractions;

internal class CircuitBreakerHandler
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _recoveryInterval;
    private int _failureCount = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private bool _isOpen = false;

    public CircuitBreakerHandler(int failureThreshold, TimeSpan recoveryInterval)
    {
        _failureThreshold = failureThreshold;
        _recoveryInterval = recoveryInterval;
    }

    public bool Handle(ErrorContext errorContext, object originalMessage)
    {
        var now = DateTime.UtcNow;

        // 回路がオープン状態で回復時間が経過した場合、ハーフオープンに
        if (_isOpen && now - _lastFailureTime > _recoveryInterval)
        {
            _isOpen = false;
            _failureCount = 0;
        }

        // 回路がオープン状態の場合、処理をスキップ
        if (_isOpen)
        {
            return false; // Skip processing
        }

        // 失敗カウントを増加
        _failureCount++;
        _lastFailureTime = now;

        // 閾値を超えた場合、回路をオープンに
        if (_failureCount >= _failureThreshold)
        {
            _isOpen = true;
        }

        return false; // Skip this message
    }
}
