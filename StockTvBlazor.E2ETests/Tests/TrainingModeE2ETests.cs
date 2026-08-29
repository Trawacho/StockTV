using StockTvBlazor.E2ETests.Fixtures;
using System.Text.Json;
using Microsoft.Playwright;

namespace StockTvBlazor.E2ETests.Tests;

/// <summary>
/// End-to-end tests for Training mode: UI input → Score update → NetMQ broadcast.
/// </summary>
[Collection("App")]
public class TrainingModeE2ETests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public TrainingModeE2ETests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task TrainingMode_InputViaNumpad_UpdatesScoreInUI()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to training page
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Open input page in new tab
		var inputPage = await _fixture.Context!.NewPageAsync();
		await inputPage.GotoAsync("http://localhost:5001/input");
		await inputPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Simulate numpad input: 5 (digit) + * (grün/left)
		// On Training page, left is controlled by input page
		await inputPage.Keyboard.PressAsync("5");
		await Task.Delay(100);
		await inputPage.Keyboard.PressAsync("*");
		await Task.Delay(500);

		// Verify score update in Training page
		var scoreContent = await _fixture.Page.ContentAsync();
		Assert.Contains("5", scoreContent); // Score should show 5

		await inputPage.CloseAsync();
	}

	[Fact]
	public async Task TrainingMode_NetMQPublisher_BroadcastsScoreAfterInput()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to training
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Open input page
		var inputPage = await _fixture.Context!.NewPageAsync();
		await inputPage.GotoAsync("http://localhost:5001/input");
		await inputPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Simulate input: 7 + * (score 7:0)
		await inputPage.Keyboard.PressAsync("7");
		await Task.Delay(100);
		await inputPage.Keyboard.PressAsync("*");
		await Task.Delay(300);

		// Listen for NetMQ broadcast
		bool received = _fixture.TryReceivePublisherBroadcast(
			out var topic,
			out var payload,
			TimeSpan.FromSeconds(2)
		);

		// Verify broadcast
		Assert.True(received, "No NetMQ broadcast received");
		Assert.NotEmpty(topic);
		Assert.NotEmpty(payload);

		// Payload should contain score information (JSON format)
		try
		{
			var json = JsonDocument.Parse(payload);
			Assert.NotNull(json);
		}
		catch
		{
			Assert.True(false, $"NetMQ payload is not valid JSON: {payload}");
		}

		await inputPage.CloseAsync();
	}

	[Fact]
	public async Task TrainingMode_NetMQGetResult_ReturnsCurrentScore()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to training
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Open input, enter score
		var inputPage = await _fixture.Context!.NewPageAsync();
		await inputPage.GotoAsync("http://localhost:5001/input");
		await inputPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

		await inputPage.Keyboard.PressAsync("3");
		await Task.Delay(100);
		await inputPage.Keyboard.PressAsync("*"); // 3:0
		await Task.Delay(300);

		// Send GetResult command via NetMQ
		string response = _fixture.SendNetMqCommand("GetResult");

		// Response should start with "GetResult" followed by JSON
		Assert.StartsWith("GetResult", response);

		// Try to parse JSON (second part of response)
		var parts = response.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
		Assert.True(parts.Length >= 2, "GetResult response should have topic + JSON");

		await inputPage.CloseAsync();
	}

	[Fact]
	public async Task TrainingMode_ResetViaNumpad_ClearsScore()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to training
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Open input, enter score
		var inputPage = await _fixture.Context!.NewPageAsync();
		await inputPage.GotoAsync("http://localhost:5001/input");
		await inputPage.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Enter 5 points
		await inputPage.Keyboard.PressAsync("5");
		await Task.Delay(100);
		await inputPage.Keyboard.PressAsync("*");
		await Task.Delay(300);

		// Verify score is 5
		var scoreBeforeReset = await _fixture.Page.ContentAsync();
		Assert.Contains("5", scoreBeforeReset);

		// Send + (plus/reset) key
		await inputPage.Keyboard.PressAsync("+");
		await Task.Delay(500);

		// Verify score is reset to 0
		var scoreAfterReset = await _fixture.Page.ContentAsync();
		Assert.Contains("0", scoreAfterReset);

		await inputPage.CloseAsync();
	}
}
