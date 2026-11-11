using Microsoft.Extensions.Logging;
using StockTvBlazor.Services;

namespace StockTvBlazor.Tests.Services;

/// <summary>
/// Befund aus dem 8-Bahnen-Test: Der erste Eintrag mit einer Ausnahme liess sich nicht als JSON schreiben
/// (Exception.TargetSite), der Schreib-Task endete, und danach kam nichts mehr in die Protokolldatei.
/// </summary>
public class FileLoggerTests
{
	private static Exception Geworfen()
	{
		try { throw new InvalidOperationException("Testfehler"); }
		catch (Exception ex) { return ex; }
	}

	[Fact]
	public void Eintrag_mit_Ausnahme_laesst_sich_als_Json_schreiben()
	{
		var ex = Geworfen();
		var json = FileLoggerProvider.ZuJson(new LogMessage(DateTime.UtcNow, LogLevel.Error, "Test", "kaputt", ex.ToString()));

		Assert.Contains("Testfehler", json);
		Assert.Contains("InvalidOperationException", json);
	}

	[Fact]
	public async Task Nach_einem_Eintrag_mit_Ausnahme_wird_weiter_protokolliert()
	{
		using var provider = new FileLoggerProvider();
		var logger = provider.CreateLogger("FileLoggerTests");
		var kennung = Guid.NewGuid().ToString("N");

		logger.LogError(Geworfen(), "mit Ausnahme {Kennung}", kennung);
		logger.LogInformation("danach {Kennung}", kennung);

		var ordner = Path.Combine(AppContext.BaseDirectory, "_logs");
		string inhalt = "";
		for (var i = 0; i < 50 && !inhalt.Contains($"danach {kennung}"); i++)
		{
			await Task.Delay(100);
			inhalt = string.Concat(Directory.GetFiles(ordner, "*.log").Select(f =>
			{
				using var s = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return new StreamReader(s).ReadToEnd();
			}));
		}

		Assert.Contains($"mit Ausnahme {kennung}", inhalt);
		Assert.Contains($"danach {kennung}", inhalt);
	}
}
