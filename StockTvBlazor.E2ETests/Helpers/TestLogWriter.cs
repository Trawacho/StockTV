using System.Text;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Helpers;

public enum LogSymbol
{
	None,
	Check,
	Info,
	Warning,
	Error
}

/// <summary>
/// Writes test logs to both xUnit ITestOutputHelper and a file simultaneously.
/// </summary>
public class TestLogWriter : IDisposable
{
	private readonly ITestOutputHelper _output;
	private StreamWriter? _fileWriter;
	private readonly string _logFilePath;
	private readonly object _lock = new();
	private readonly DateTime _startTime = DateTime.Now;
	private int _phaseCount;
	private int _successCount;
	private int _warningCount;
	private readonly int _randomSeed;

	public TestLogWriter(ITestOutputHelper output, int randomSeed, string? logDirectory = null)
	{
		_output = output;
		_randomSeed = randomSeed;
		_logDirectory = logDirectory ?? Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "TestResults");
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
			WriteLine($"Random Seed für diesen Testlauf: {_randomSeed}");
			WriteLine("");
		}
		catch (Exception ex)
		{
			_output.WriteLine($"Failed to create log file: {ex.Message}");
		}
	}

	private readonly string _logDirectory;

	public void WriteLn(string phase, string message, LogSymbol symbol = LogSymbol.None)
	{
		var symbolChar = GetSymbolChar(symbol);
		string line;

		if (string.IsNullOrEmpty(symbolChar))
		{
			// Keine Symbol: konsistent 2 Spaces nach Pipe
			line = $"[{DateTime.Now:HH:mm:ss.fff}] {phase,-8} | {message}";
		}
		else
		{
			// Mit Symbol: Symbol + 1 Space nach Pipe
			line = $"[{DateTime.Now:HH:mm:ss.fff}] {phase,-8} | {symbolChar} {message}";
		}

		if (symbol == LogSymbol.Check)
			_successCount++;
		else if (symbol == LogSymbol.Warning)
			_warningCount++;

		WriteLine(line);
	}

	private string GetSymbolChar(LogSymbol symbol) => symbol switch
	{
		LogSymbol.None => "",
		LogSymbol.Check => "✓",
		LogSymbol.Info => "→",
		LogSymbol.Warning => "!",
		LogSymbol.Error => "✗",
		_ => ""
	};

	public void WritePhaseStart(string phase, string description)
	{
		_phaseCount++;
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
				try
				{
					_output.WriteLine(message);
				}
				catch
				{
					// ITestOutputHelper might not be active after test ends
				}
				_fileWriter?.WriteLine(message);
				//Console.WriteLine(message);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ERROR] Failed to write log: {ex.Message}");
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
					// Write summary directly to file (don't use WriteLine to avoid ITestOutputHelper issues)
					var duration = DateTime.Now - _startTime;
					_fileWriter.WriteLine("");
					_fileWriter.WriteLine("═══════════════════════════════════════════════════════");
					_fileWriter.WriteLine("  TEST SUMMARY");
					_fileWriter.WriteLine("═══════════════════════════════════════════════════════");
					_fileWriter.WriteLine($"Phases:         {_phaseCount}");
					_fileWriter.WriteLine($"Success Checks: {_successCount}");
					_fileWriter.WriteLine($"Warnings:       {_warningCount}");
					_fileWriter.WriteLine($"Duration:       {duration.TotalSeconds:F2}s");
					_fileWriter.WriteLine($"Log File:       {_logFilePath}");
					_fileWriter.WriteLine("═══════════════════════════════════════════════════════");
					_fileWriter.WriteLine($"Log ended at {DateTime.Now:O}");

					_fileWriter.Flush();
					_fileWriter.Dispose();

					Console.WriteLine($"✓ Log file saved to: {_logFilePath}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error closing log file: {ex.Message}");
			}
		}
	}
}
