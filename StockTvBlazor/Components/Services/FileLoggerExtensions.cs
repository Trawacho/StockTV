using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace StockTvBlazor.Components.Services;

public static class FileLoggerExtensions
{
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<FileLoggerProvider>();
        builder.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<FileLoggerProvider>());
        return builder;
    }
}
