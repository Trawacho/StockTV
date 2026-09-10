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
	private PublisherSubscriberMessageQueue? _publisherMessageQueue;

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

		// Initialize NetMQ sockets (give app more time to bind NetMQ sockets)
		await Task.Delay(3000);
		InitializeNetMQ();

		// Launch Playwright (Headless can be overridden with PLAYWRIGHT_HEADLESS=false env var)
		_playwright = await Playwright.CreateAsync();
		bool headless = !string.Equals(Environment.GetEnvironmentVariable("PLAYWRIGHT_HEADLESS"), "false", StringComparison.OrdinalIgnoreCase);

		_browser = await _playwright.Chromium.LaunchAsync(new()
		{
			Headless = headless,
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

		// Cleanup NetMQ (careful with disposal order)
		try
		{
			// Clear message queue
			_publisherMessageQueue?.Clear();

			// Stop poller first, then wait for thread to finish
			if (_poller?.IsRunning == true)
			{
				_poller.Stop();
				await Task.Delay(500);
			}

			// Wait for poller thread to finish before disposing sockets
			if (_pollerThread?.IsAlive == true)
				_pollerThread.Join(2000);

			// Now dispose sockets
			_publisherSubscriber?.Dispose();
			_netMqRequester?.Dispose();

			// Finally dispose poller
			_poller?.Dispose();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"NetMQ cleanup error: {ex.Message}");
		}

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
		// Find StockTvBlazor.csproj by traversing up from current directory
		string projectPath = FindProjectPath();

		if (!File.Exists(projectPath))
			throw new FileNotFoundException($"Project file not found: {projectPath}");

		System.Diagnostics.Debug.WriteLine($"Starting app from: {projectPath}");

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
		System.Diagnostics.Debug.WriteLine($"App process started (PID: {_appProcess.Id})");

		// Give process time to start
		await Task.Delay(3000);

		if (_appProcess.HasExited)
		{
			var output = await _appProcess.StandardOutput.ReadToEndAsync();
			var error = await _appProcess.StandardError.ReadToEndAsync();
			System.Diagnostics.Debug.WriteLine($"App stdout: {output}");
			System.Diagnostics.Debug.WriteLine($"App stderr: {error}");
			throw new InvalidOperationException($"App failed to start. Stderr: {error}");
		}

		System.Diagnostics.Debug.WriteLine("App started successfully, waiting for readiness...");
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

	private string FindProjectPath()
	{
		// Start from current directory and traverse up to find StockTvBlazor.csproj
		var current = new DirectoryInfo(Directory.GetCurrentDirectory());

		while (current != null)
		{
			var projectFile = Path.Combine(current.FullName, "StockTvBlazor", "StockTvBlazor.csproj");
			if (File.Exists(projectFile))
				return projectFile;

			current = current.Parent;
		}

		throw new FileNotFoundException("Could not find StockTvBlazor.csproj in parent directories");
	}

	private void InitializeNetMQ()
	{
		try
		{
			// Initialize message queue
			_publisherMessageQueue = new PublisherSubscriberMessageQueue();
			System.Diagnostics.Debug.WriteLine("NetMQ message queue initialized");

			// Publisher subscriber (PUB-SUB, port 4748)
			_publisherSubscriber = new SubscriberSocket();
			_publisherSubscriber.Connect(PublisherUrl);
			_publisherSubscriber.Subscribe(""); // Subscribe to all topics
			System.Diagnostics.Debug.WriteLine("NetMQ Publisher subscriber connected");

			// Requester socket (REQ-REP, port 4747)
			_netMqRequester = new RequestSocket();
			_netMqRequester.Connect(RequesterUrl);
			System.Diagnostics.Debug.WriteLine("NetMQ Requester socket connected");

			// Start poller on background thread
			if (_publisherSubscriber != null)
			{
				_poller = new NetMQPoller();

				// Register receive handler to record messages
				_publisherSubscriber.ReceiveReady += (sender, args) =>
				{
					try
					{
						var message = new NetMQMessage();
						if (args.Socket.TryReceiveMultipartMessage(TimeSpan.FromMilliseconds(100), ref message))
						{
							var topic = "";
							var payload = "";
							var frameCount = message.FrameCount;

							if (message.FrameCount >= 2)
							{
								// Standard multipart: topic in frame 0, payload in frame 1
								topic = Encoding.UTF8.GetString(message[0].Buffer);
								payload = Encoding.UTF8.GetString(message[1].Buffer);
							}
							else if (message.FrameCount == 1)
							{
								// Single frame: might contain topic\0payload or just payload
								var fullContent = Encoding.UTF8.GetString(message[0].Buffer);
								var parts = fullContent.Split('\0');
								if (parts.Length >= 2)
								{
									topic = parts[0];
									payload = parts[1];
								}
								else
								{
									payload = fullContent;
								}
							}

							// Record message in queue
							if (_publisherMessageQueue != null)
							{
								_publisherMessageQueue.Enqueue(new PublisherSubscriberMessage
								{
									Topic = topic,
									Payload = payload,
									Timestamp = DateTime.UtcNow,
									NetMqFrameCount = frameCount	
								});
							}
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"NetMQ message receive error: {ex.Message}");
					}
				};

				_poller.Add(_publisherSubscriber);

				_pollerThread = new Thread(() =>
				{
					try
					{
						_poller.Run();
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"NetMQ Poller error: {ex.Message}");
					}
				})
				{
					IsBackground = true,
					Name = "E2E-NetMQ-Poller"
				};
				_pollerThread.Start();
				System.Diagnostics.Debug.WriteLine("NetMQ Poller started");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("ERROR: _publisherSubscriber is null before creating poller!");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"NetMQ initialization error: {ex.Message}\n{ex.StackTrace}");
			throw;
		}
	}

	/// <summary>
	/// Debug helper: Take screenshot if page content is empty/suspect.
	/// </summary>
	public async Task DebugScreenshotAsync(string testName)
	{
		if (Page == null)
			return;

		try
		{
			var screenshotPath = Path.Combine(Path.GetTempPath(), $"e2e-debug-{testName}-{DateTime.Now:yyyyMMdd-HHmmss}.png");
			await Page.ScreenshotAsync(new() { Path = screenshotPath });
			System.Diagnostics.Debug.WriteLine($"Screenshot saved: {screenshotPath}");
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Screenshot failed: {ex.Message}");
		}
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
	/// Send NetMQ command with raw byte payload (for SetSettings) and receive full response message.
	/// </summary>
	public NetMQMessage SendNetMqRaw(string topic, byte[]? payload = null)
	{
		if (_netMqRequester == null)
			throw new InvalidOperationException("NetMQ requester not initialized");

		var message = new NetMQMessage();
		message.Append(Encoding.UTF8.GetBytes(topic));
		if (payload != null && payload.Length > 0)
			message.Append(payload);

		_netMqRequester.SendMultipartMessage(message);

		// Receive response with timeout
		var response = new NetMQMessage();
		if (_netMqRequester.TryReceiveMultipartMessage(TimeSpan.FromSeconds(3), ref response))
		{
			return response;
		}

		throw new TimeoutException("NetMQ response timeout");
	}

	/// <summary>
	/// Get all recorded Publisher-Subscriber messages with the specified topic. GetResult or Alive messages can be retrieved this way.
	/// </summary>
	public IReadOnlyList<PublisherSubscriberMessage> GetPublisherMessagesByTopic(string topic)
	{
		if (_publisherMessageQueue == null)
			throw new InvalidOperationException("Publisher message queue not initialized");

		return _publisherMessageQueue.GetMessagesByTopic(topic);
	}

	/// <summary>
	/// Get all recorded Publisher-Subscriber messages.
	/// </summary>
	public IReadOnlyList<PublisherSubscriberMessage> GetAllPublisherMessages()
	{
		if (_publisherMessageQueue == null)
			throw new InvalidOperationException("Publisher message queue not initialized");

		return _publisherMessageQueue.GetAllMessages();
	}

	/// <summary>
	/// Clear all recorded Publisher-Subscriber messages.
	/// </summary>
	public void ClearPublisherMessages()
	{
		if (_publisherMessageQueue == null)
			throw new InvalidOperationException("Publisher message queue not initialized");

		_publisherMessageQueue.Clear();
	}

	/// <summary>
	/// Get the number of messages currently recorded in the Publisher-Subscriber queue.
	/// </summary>
	public int GetPublisherMessageCount()
	{
		if (_publisherMessageQueue == null)
			throw new InvalidOperationException("Publisher message queue not initialized");

		return _publisherMessageQueue.Count;
	}
}
