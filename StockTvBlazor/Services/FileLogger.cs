using System.Threading.Channels;

namespace StockTvBlazor.Services;

public class FileLogger : ILogger
{
    private readonly string _category;
    private readonly Channel<LogMessage> _channel;
    private readonly FileLoggerProvider _provider;

    public FileLogger(string category, Channel<LogMessage> channel, FileLoggerProvider provider)
    {
        _category = category;
        _channel = channel;
        _provider = provider;
    }

	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
	{
	     return default!;
	}

	public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!_provider.Enabled)
            return;

        var msg = new LogMessage(
            DateTime.UtcNow,
            logLevel,
            _category,
            formatter(state, exception),
            exception
        );

        _channel.Writer.TryWrite(msg);
    }
}
