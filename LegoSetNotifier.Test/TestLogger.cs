using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace LegoSetNotifier.Test
{
    public class TestLogger : ILogger
    {
        private ConcurrentQueue<LogMessage> logMessages = new ConcurrentQueue<LogMessage>();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return (state as IDisposable);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var logMessage = new LogMessage()
            {
                Level = logLevel,
                Message = formatter(state, exception),
                Exception = exception,
            };

            this.logMessages.Enqueue(logMessage);
        }

        public IReadOnlyList<LogMessage> GetLogs(LogLevel level)
        {
            return this.logMessages.Where(l => l.Level == level).ToList();
        }

        public class LogMessage
        {
            public LogLevel Level { get; set; }

            public string Message { get; set; } = string.Empty;

            public Exception? Exception { get; set; }
        }
    }
}
