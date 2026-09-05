using System.Text;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Helpers;

/// <summary>
/// Writes test logs to both xUnit ITestOutputHelper and a file simultaneously.
/// </summary>
public class TestLogWriter : IDisposable
{
	private readonly ITestOutputHelper _output;
	private StreamWriter? _fileWriter;
	private readonly string _logFilePath;
	private readonly object _lock = new();

	public TestLogWriter(ITestOutputHelper output, string? logDirectory = null)
	{
		_output = output;
		_logDirectory = logDirectory ?? Path.Combine(Path.GetTempPath(), "stocktv-e2e-logs");
		_logFilePath = Path.Combine(_logDirectory, $"e2e-test-{DateTime.Now:yyyyMMdd-HHmmss}.log");

		// Ensure log directory exists
		Directory.CreateDirectory(_logDirectory);

		try
		{
			_fileWriter = new StreamWriter(_logFilePath, append: false, Encoding.UTF8, bufferSize: 4096)
			{
				AutoFlush = true
			};
			WriteLine($"Log started at {DateTime.Now:O}");
			WriteLine($"Log file: {_logFilePath}");
		}
		catch (Exception ex)
		{
			_output.WriteLine($"Failed to create log file: {ex.Message}");
		}
	}

	private readonly string _logDirectory;

	public void WriteLn(string phase, string message, string symbol = "✓")
	{
		var line = $"[{DateTime.Now:HH:mm:ss.fff}] {phase,-8} | {symbol} {message}";
		WriteLine(line);
	}

	public void WritePhaseStart(string phase, string description)
	{
		WriteLine("");
		WriteLine("═══════════════════════════════════════════════════════");
		WriteLine($"  {phase}: {description}");
		WriteLine("═══════════════════════════════════════════════════════");
	}

	public void WritePhaseEnd(string phase)
	{
		WriteLine($"✓ {phase} erfolgreich beendet");
		WriteLine("");
	}

	public void WriteLine(string message)
	{
		lock (_lock)
		{
			try
			{
				_output.WriteLine(message);
				_fileWriter?.WriteLine(message);
				Console.WriteLine(message);
			}
			catch (Exception ex)
			{
				_output.WriteLine($"[ERROR] Failed to write log: {ex.Message}");
			}
		}
	}

	public void Dispose()
	{
		lock (_lock)
		{
			try
			{
				if (_fileWriter != null)
				{
					WriteLine($"Log ended at {DateTime.Now:O}");
					_fileWriter.Flush();
					_fileWriter.Dispose();
					_output.WriteLine($"✓ Log file saved to: {_logFilePath}");
				}
			}
			catch (Exception ex)
			{
				_output.WriteLine($"Error closing log file: {ex.Message}");
			}
		}
	}
}
