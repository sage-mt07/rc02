using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Kafka.Ksql.Linq.Core.Extensions;
/// <summary>
/// ILoggerFactory generalized extension methods (Option 2 complete version)
/// Rationale: remove KafkaContext dependency and keep proper layering in Core
/// </summary>
public static class LoggerFactoryExtensions
{
    /// <summary>
    /// Create a type-safe logger from ILoggerFactory; returns NullLogger if null
    /// </summary>
    /// <typeparam name="T">ロガーのカテゴリ型</typeparam>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <returns>ILogger&lt;T&gt; または NullLogger&lt;T&gt;.Instance</returns>
    public static ILogger<T> CreateLoggerOrNull<T>(this ILoggerFactory? loggerFactory)
    {
        return loggerFactory?.CreateLogger<T>() ?? NullLogger<T>.Instance;
    }

    /// <summary>
    /// Create a logger by category name
    /// </summary>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="categoryName">カテゴリ名</param>
    /// <returns>ILogger または NullLogger.Instance</returns>
    public static ILogger CreateLoggerOrNull(this ILoggerFactory? loggerFactory, string categoryName)
    {
        return loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Create a logger by Type category
    /// </summary>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="type">カテゴリのType</param>
    /// <returns>ILogger または NullLogger.Instance</returns>
    public static ILogger CreateLoggerOrNull(this ILoggerFactory? loggerFactory, Type type)
    {
        return loggerFactory?.CreateLogger(type) ?? NullLogger.Instance;
    }

    /// <summary>
    /// Debug logging with backward compatibility (generic)
    /// </summary>
    /// <typeparam name="T">ロガーのカテゴリ型</typeparam>
    /// <param name="logger">ロガー</param>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="enableLegacyLogging">レガシーログ有効フラグ</param>
    /// <param name="message">メッセージテンプレート</param>
    /// <param name="args">引数</param>
    public static void LogDebugWithLegacySupport<T>(this ILogger<T> logger,
        ILoggerFactory? loggerFactory, bool enableLegacyLogging,
        string message, params object[] args)
    {
        // If a modern LoggerFactory is provided
        if (loggerFactory != null)
        {
            logger.LogDebug(message, args);
        }
        // Backward compatibility: legacy EnableDebugLogging flag
        else if (enableLegacyLogging)
        {
            Console.WriteLine($"[DEBUG] {string.Format(message, args)}");
        }
    }

    /// <summary>
    /// Debug logging with backward compatibility (non-generic)
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="enableLegacyLogging">レガシーログ有効フラグ</param>
    /// <param name="message">メッセージテンプレート</param>
    /// <param name="args">引数</param>
    public static void LogDebugWithLegacySupport(this ILogger logger,
        ILoggerFactory? loggerFactory, bool enableLegacyLogging,
        string message, params object[] args)
    {
        // If a modern LoggerFactory is provided
        if (loggerFactory != null)
        {
            logger.LogDebug(message, args);
        }
        // Backward compatibility: legacy EnableDebugLogging flag
        else if (enableLegacyLogging)
        {
            Console.WriteLine($"[DEBUG] {string.Format(message, args)}");
        }
    }

    /// <summary>
    /// Information logging with backward compatibility (generic)
    /// </summary>
    /// <typeparam name="T">ロガーのカテゴリ型</typeparam>
    /// <param name="logger">ロガー</param>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="enableLegacyLogging">レガシーログ有効フラグ</param>
    /// <param name="message">メッセージテンプレート</param>
    /// <param name="args">引数</param>
    public static void LogInformationWithLegacySupport<T>(this ILogger<T> logger,
        ILoggerFactory? loggerFactory, bool enableLegacyLogging,
        string message, params object[] args)
    {
        if (loggerFactory != null)
        {
            logger.LogInformation(message, args);
        }
        else if (enableLegacyLogging)
        {
            Console.WriteLine($"[INFO] {string.Format(message, args)}");
        }
    }

    /// <summary>
    /// Warning logging with backward compatibility (generic)
    /// </summary>
    /// <typeparam name="T">ロガーのカテゴリ型</typeparam>
    /// <param name="logger">ロガー</param>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="enableLegacyLogging">レガシーログ有効フラグ</param>
    /// <param name="message">メッセージテンプレート</param>
    /// <param name="args">引数</param>
    public static void LogWarningWithLegacySupport<T>(this ILogger<T> logger,
        ILoggerFactory? loggerFactory, bool enableLegacyLogging,
        string message, params object[] args)
    {
        if (loggerFactory != null)
        {
            logger.LogWarning(message, args);
        }
        else if (enableLegacyLogging)
        {
            Console.WriteLine($"[WARNING] {string.Format(message, args)}");
        }
    }

    /// <summary>
    /// Error logging with backward compatibility (generic)
    /// </summary>
    /// <typeparam name="T">ロガーのカテゴリ型</typeparam>
    /// <param name="logger">ロガー</param>
    /// <param name="exception">例外</param>
    /// <param name="loggerFactory">ロガーファクトリ（null許可）</param>
    /// <param name="enableLegacyLogging">レガシーログ有効フラグ</param>
    /// <param name="message">メッセージテンプレート</param>
    /// <param name="args">引数</param>
    public static void LogErrorWithLegacySupport<T>(this ILogger<T> logger,
        Exception exception, ILoggerFactory? loggerFactory, bool enableLegacyLogging,
        string message, params object[] args)
    {
        if (loggerFactory != null)
        {
            logger.LogError(exception, message, args);
        }
        else if (enableLegacyLogging)
        {
            Console.WriteLine($"[ERROR] {string.Format(message, args)}");
            Console.WriteLine($"[ERROR] Exception: {exception.Message}");
        }
    }

    /// <summary>
    /// Create an ILoggerFactory from the Logging section in appsettings.json.
    /// </summary>
    /// <param name="configuration">Configuration</param>
    /// <returns>Configured ILoggerFactory</returns>
    public static ILoggerFactory CreateLoggerFactory(this IConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        return LoggerFactory.Create(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
        });
    }
}
