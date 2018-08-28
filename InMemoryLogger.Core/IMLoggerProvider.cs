using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InMemoryLogger.Core
{

    public class IMLoggerProvider : ILoggerProvider
    {
        private readonly IMLoggerStore _store;
        private readonly IMLoggerOptions _options;

        public IMLoggerProvider(IMLoggerStore store, IOptions<IMLoggerOptions> options)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _store = store;
            _options = options.Value;
        }

        public ILogger CreateLogger(string name)
        {
            return new InMemoryLogger(name, _options, _store);
        }

        public void Dispose()
        {
        }
    }


    public class InMemoryLogger : ILogger
    {
        private readonly string _name;
        private readonly IMLoggerOptions _options;
        private readonly IMLoggerStore _store;

        public InMemoryLogger(string name, IMLoggerOptions options, IMLoggerStore store)
        {
            _name = name;
            _options = options;
            _store = store;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                          Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) || (state == null && exception == null))
            {
                return;
            }
            LogInfo info = new LogInfo()
            {
                ActivityContext = GetCurrentActivityContext(),
                Name = _name,
                EventID = eventId.Id,
                Severity = logLevel,
                Exception = exception,
                State = state,
                Message = formatter == null ? state.ToString() : formatter(state, exception),
                Time = DateTimeOffset.UtcNow
            };
            if (IMLoggerScope.Current != null)
            {
                IMLoggerScope.Current.Node.Messages.Add(info);
            }
            // The log does not belong to any scope - create a new context for it
            else
            {
                var context = GetNewActivityContext();
                context.RepresentsScope = false;  // mark as a non-scope log
                context.Root = new IMLoggerScopeNode();
                context.Root.Messages.Add(info);
                _store.AddActivity(context);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _options.Filter(_name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            var scope = new IMLoggerScope(_name, state);
            scope.Context = IMLoggerScope.Current?.Context ?? GetNewActivityContext();
            return IMLoggerScope.Push(scope, _store);
        }

        private ActivityContext GetNewActivityContext()
        {
            return new ActivityContext()
            {
                Id = Guid.NewGuid(),
                Time = DateTimeOffset.UtcNow,
                RepresentsScope = true
            };
        }

        private ActivityContext GetCurrentActivityContext()
        {
            return IMLoggerScope.Current?.Context ?? GetNewActivityContext();
        }
    }
    
    public class LogInfo
    {
        public ActivityContext ActivityContext { get; set; }

        public string Name { get; set; }

        public object State { get; set; }

        public Exception Exception { get; set; }

        public string Message { get; set; }

        public LogLevel Severity { get; set; }

        public int EventID { get; set; }

        public DateTimeOffset Time { get; set; }
    }
}
