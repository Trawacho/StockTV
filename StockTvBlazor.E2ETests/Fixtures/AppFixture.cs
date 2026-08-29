using System.Diagnostics;
using System.Net.Http;
using Microsoft.Playwright;
using NetMQ;
using NetMQ.Sockets;
using System.Text;
using System.Text.Json;

namespace StockTvBlazor.E2ETests.Fixtures;

public class AppFixture : IAsyncLifetime
{
	private Process? _appProcess;
	private IBrowser? _browser;
	private IPlaywright? _playwright;
	public IPage? Page { get; private set; }
	public IBrowserContext? Context { get; private set; }

	// NetMQ sockets
	private SubscriberSocket? _publisherSubscriber;
	private RequestSocket? _netMqRequester;
	private NetMQPoller? _poller;
	private Thread? _pollerThread;

	private const string AppUrl = "http://localhost:5001";
	private const string PublisherUrl = "tcp://127.0.0.1:4748";
	private const string RequesterUrl = "tcp://127.0.0.1:4747";
	private const int StartupTimeoutMs = 30000;
	private const int ReadinessCheckTimeoutMs = 60000;

	public async Task InitializeAsync()
	{
		// Start the StockTV app in a subprocess
		await StartAppAsync();

		// Wait for app to be ready
		await WaitForAppReadinessAsync();

		// Initialize NetMQ sockets (give app time to bind)
		await Task.Delay(1000);
		InitializeNetMQ();

		// Launch Playwright
		_playwright = await Playwright.CreateAsync();
		_browser = await _playwright.Chromium.LaunchAsync(new()
		{
			Headless = true,
			Args = new[] { "--disable-gpu", "--no-sandbox" }
		});

		Context = await _browser.NewContextAsync();
		Page = await Context.NewPageAsync();
	}

	public async Task DisposeAsync()
	{
		// Cleanup Playwright
		if (Page != null)
			await Page.CloseAsync();

		if (Context != null)
			await Context.CloseAsync();

		if (_browser != null)
			await _browser.CloseAsync();

		_playwright?.Dispose();

		// Cleanup NetMQ
		try
		{
			if (_poller?.IsRunning == true)
				_poller.StopAsync();
			_publisherSubscriber?.Dispose();
			_netMqRequester?.Dispose();
			_poller?.Dispose();

			if (_pollerThread?.IsAlive == true)
				_pollerThread.Join(2000);
		}
		catch { }

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

	private void InitializeNetMQ()
	{
		// Publisher subscriber (PUB-SUB, port 4748)
		_publisherSubscriber = new SubscriberSocket();
		_publisherSubscriber.Connect(PublisherUrl);
		_publisherSubscriber.Subscribe(""); // Subscribe to all topics

		// Requester socket (REQ-REP, port 4747)
		_netMqRequester = new RequestSocket();
		_netMqRequester.Connect(RequesterUrl);

		// Start poller on background thread
		_poller = new NetMQPoller { _publisherSubscriber };
		_pollerThread = new Thread(() => _poller.Run())
		{
			IsBackground = true,
			Name = "E2E-NetMQ-Poller"
		};
		_pollerThread.Start();
	}

	/// <summary>
	/// Send a NetMQ command and receive response (REQ-REP).
	/// </summary>
	public string SendNetMqCommand(string topic, string? payload = null)
	{
		if (_netMqRequester == null)
			throw new InvalidOperationException("NetMQ requester not initialized");

		var message = new NetMQMessage();
		message.Append(Encoding.UTF8.GetBytes(topic));
		if (!string.IsNullOrEmpty(payload))
			message.Append(Encoding.UTF8.GetBytes(payload));

		_netMqRequester.SendMultipartMessage(message);

		// Receive response with timeout
		if (_netMqRequester.TryReceiveMultipartMessage(TimeSpan.FromSeconds(3), ref message))
		{
			if (message.FrameCount > 0)
				return Encoding.UTF8.GetString(message[0].Buffer);
		}

		throw new TimeoutException("NetMQ response timeout");
	}

	/// <summary>
	/// Receive next broadcast from publisher (non-blocking, timeout 1s).
	/// </summary>
	public bool TryReceivePublisherBroadcast(out string topic, out string payload, TimeSpan? timeout = null)
	{
		topic = "";
		payload = "";

		if (_publisherSubscriber == null)
			return false;

		timeout ??= TimeSpan.FromSeconds(1);
		var message = new NetMQMessage();

		if (_publisherSubscriber.TryReceiveMultipartMessage(timeout.Value, ref message))
		{
			if (message.FrameCount >= 2)
			{
				topic = Encoding.UTF8.GetString(message[0].Buffer);
				payload = Encoding.UTF8.GetString(message[1].Buffer);
				return true;
			}
		}

		return false;
	}
}
