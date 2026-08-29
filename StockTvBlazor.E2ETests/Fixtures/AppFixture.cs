using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;

namespace StockTvBlazor.E2ETests.Fixtures;

public class AppFixture : IAsyncLifetime
{
	private Process? _appProcess;
	private IBrowser? _browser;
	private IPlaywright? _playwright;
	public IPage? Page { get; private set; }
	public IBrowserContext? Context { get; private set; }

	private const string AppUrl = "http://localhost:5001";
	private const int StartupTimeoutMs = 30000;
	private const int ReadinessCheckTimeoutMs = 60000;

	public async Task InitializeAsync()
	{
		// Start the StockTV app in a subprocess
		await StartAppAsync();

		// Wait for app to be ready
		await WaitForAppReadinessAsync();

		// Launch Playwright
		_playwright = await Playwright.CreateAsync();
		_browser = await _playwright.Chromium.LaunchAsync(new BrowserLaunchOptions
		{
			Headless = true,
			Args = new[] { "--disable-gpu", "--no-sandbox" }
		});

		Context = await _browser.NewContextAsync();
		Page = await Context.NewPageAsync();
	}

	public async Task DisposeAsync()
	{
		if (Page != null)
			await Page.CloseAsync();

		if (Context != null)
			await Context.CloseAsync();

		if (_browser != null)
			await _browser.CloseAsync();

		_playwright?.Dispose();

		// Kill the app process
		if (_appProcess != null)
		{
			try
			{
				if (!_appProcess.HasExited)
				{
					_appProcess.Kill();
					_appProcess.WaitForExit(5000);
				}
			}
			catch { }
			_appProcess.Dispose();
		}
	}

	private async Task StartAppAsync()
	{
		var projectPath = Path.Combine(
			Directory.GetCurrentDirectory(),
			"..", "..", "StockTvBlazor", "StockTvBlazor.csproj");

		if (!File.Exists(projectPath))
			throw new FileNotFoundException($"Project file not found: {projectPath}");

		_appProcess = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"run --project \"{projectPath}\" --urls {AppUrl}",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				EnvironmentVariables =
				{
					{ "ASPNETCORE_ENVIRONMENT", "Development" },
					{ "DOTNET_ENVIRONMENT", "Development" }
				}
			}
		};

		_appProcess.Start();

		// Give process time to start
		await Task.Delay(2000);

		if (_appProcess.HasExited)
		{
			var error = await _appProcess.StandardError.ReadToEndAsync();
			throw new InvalidOperationException($"App failed to start: {error}");
		}
	}

	private async Task WaitForAppReadinessAsync()
	{
		var startTime = DateTime.UtcNow;
		var timeout = TimeSpan.FromMilliseconds(ReadinessCheckTimeoutMs);

		using (var client = new HttpClient())
		{
			while (DateTime.UtcNow - startTime < timeout)
			{
				try
				{
					var response = await client.GetAsync($"{AppUrl}/", HttpCompletionOption.ResponseHeadersRead);
					if (response.IsSuccessStatusCode)
						return;
				}
				catch { }

				await Task.Delay(500);
			}
		}

		throw new TimeoutException($"App did not respond within {ReadinessCheckTimeoutMs}ms");
	}
}
