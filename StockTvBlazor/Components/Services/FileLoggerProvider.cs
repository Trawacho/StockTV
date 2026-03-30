using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Channels;

namespace StockTvBlazor.Components.Services;

public class FileLoggerProvider : ILoggerProvider
{
    public bool Enabled { get; set; } = true;

    private readonly string _logDirectory;
    private readonly Channel<LogMessage> _channel;
    private readonly CancellationTokenSource _cts = new();
    private readonly int _maxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly int _retentionDays = 30;

    public FileLoggerProvider()
    {
        _logDirectory = Path.Combine(AppContext.BaseDirectory, "_logs");
        Directory.CreateDirectory(_logDirectory);

        CleanupOldFiles();

        _channel = Channel.CreateUnbounded<LogMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        Task.Run(ProcessQueueAsync);
    }

    public ILogger CreateLogger(string categoryName)
        => new FileLogger(categoryName, _channel, this);

    public void Dispose()
    {
        _cts.Cancel();
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            var msg = await _channel.Reader.ReadAsync(_cts.Token);
            await WriteLogAsync(msg);
        }
    }

    private async Task WriteLogAsync(LogMessage msg)
    {
        string file = GetCurrentLogFile();

        var json = JsonSerializer.Serialize(msg) + Environment.NewLine;

        await File.AppendAllTextAsync(file, json);
    }

    private string GetCurrentLogFile()
    {
        string baseName = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}");
        string file = baseName + ".log";

        if (File.Exists(file) && new FileInfo(file).Length > _maxFileSizeBytes)
        {
            int index = 1;
            while (File.Exists($"{baseName}_{index}.log") &&
                   new FileInfo($"{baseName}_{index}.log").Length > _maxFileSizeBytes)
            {
                index++;
            }

            file = $"{baseName}_{index}.log";
        }

        return file;
    }

    private void CleanupOldFiles()
    {
        foreach (var file in Directory.GetFiles(_logDirectory, "*.log"))
        {
            if (File.GetCreationTimeUtc(file) < DateTime.UtcNow.AddDays(-_retentionDays))
            {
                File.Delete(file);
            }
        }
    }
}
