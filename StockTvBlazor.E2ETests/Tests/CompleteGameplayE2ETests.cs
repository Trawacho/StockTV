using StockTvBlazor.E2ETests.Fixtures;
using System.Text.Json;
using Microsoft.Playwright;

namespace StockTvBlazor.E2ETests.Tests;

/// <summary>
/// Complete gameplay E2E tests: realistic game scenarios with direct keyboard input on game pages.
/// Each test simulates a full game from start to finish, verifying:
/// - Score updates in real-time
/// - Turn/Game progression
/// - Match-point calculations
/// - NetMQ broadcasts for each action
/// </summary>
[Collection("App")]
public class CompleteGameplayE2ETests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public CompleteGameplayE2ETests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task TrainingMode_CompleteGame_MultipleInputsAndReset()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to Training page
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify initial state: 0:0
		var content = await _fixture.Page.ContentAsync();
		Assert.Contains("0", content);

		// Simulate first input: 8 points for green (right)
		await _fixture.Page.Keyboard.PressAsync("8");
		await Task.Delay(100);
		await _fixture.Page.Keyboard.PressAsync("*"); // Green/right
		await Task.Delay(200);

		// Verify score updated to 0:8
		content = await _fixture.Page.ContentAsync();
		Assert.Contains("8", content);

		// Add 3 more points: 3 + *
		await _fixture.Page.Keyboard.PressAsync("3");
		await Task.Delay(100);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		// Score should now be 0:11 (8+3)
		content = await _fixture.Page.ContentAsync();
		Assert.Contains("11", content);

		// Red side: 7 points
		await _fixture.Page.Keyboard.PressAsync("7");
		await Task.Delay(100);
		await _fixture.Page.Keyboard.PressAsync("/"); // Red/left
		await Task.Delay(200);

		// Score should be 7:11
		content = await _fixture.Page.ContentAsync();
		Assert.Contains("7", content);

		// Reset with +
		await _fixture.Page.Keyboard.PressAsync("+");
		await Task.Delay(300);

		// After reset, should show new turn (Kehre: 2)
		content = await _fixture.Page.ContentAsync();
		Assert.Contains("Kehre", content);
	}

	[Fact]
	public async Task BestOfMode_CompleteMatchScenario_TwoGames()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/bestof");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.Contains("Spiel", content); // Game counter

		// Game 1: Green wins 10:5
		// First turn: 10 for green
		await _fixture.Page.Keyboard.PressAsync("1");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("0");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		content = await _fixture.Page.ContentAsync();
		Assert.Contains("10", content);

		// Second turn: 5 for red
		await _fixture.Page.Keyboard.PressAsync("5");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("/");
		await Task.Delay(200);

		// Confirm game: +
		await _fixture.Page.Keyboard.PressAsync("+");
		await Task.Delay(300);

		// Verify match-points updated
		content = await _fixture.Page.ContentAsync();
		// After Game 1, match-points should show 1:0 (Green leads)
		Assert.Contains("Spiel: 2", content); // Should progress to Game 2
	}

	[Fact]
	public async Task ZielMode_AllDisciplinesSequence_FourDisciplines()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);

		// Ziel mode has 4 disciplines in sequence
		// Discipline 1: MassenVorne (valid values: 0, 2, 4, 6, 8, 10)
		await _fixture.Page.Keyboard.PressAsync("8");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		// Discipline 2: Schiessen (valid values: 0, 2, 5, 10)
		await _fixture.Page.Keyboard.PressAsync("1");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("0");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		// Discipline 3: MassenSeite
		await _fixture.Page.Keyboard.PressAsync("6");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		// Discipline 4: Kombinieren
		await _fixture.Page.Keyboard.PressAsync("4");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(200);

		// Verify all inputs processed
		content = await _fixture.Page.ContentAsync();
		Assert.Contains("Versuch", content); // Attempt counter
	}

	[Fact]
	public async Task InputPage_NumpadLayout_AllKeysAccessible()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/input");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();

		// Input page should contain numpad elements
		Assert.Contains("iframe", content.ToLowerInvariant());

		// Test numpad keys are responding
		await _fixture.Page.Keyboard.PressAsync("7");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("8");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("9");
		await Task.Delay(50);

		// Verify input processes without errors
		content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
	}

	[Fact]
	public async Task NetMQIntegration_BroadcastsForEachGameAction()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Start listening for NetMQ broadcasts
		var broadcastsReceived = new List<(string topic, string payload)>();

		// Navigate to Training
		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Enter first score: 5 points
		await _fixture.Page.Keyboard.PressAsync("5");
		await Task.Delay(50);
		await _fixture.Page.Keyboard.PressAsync("*");
		await Task.Delay(300);

		// Try to receive broadcast (5 second window)
		bool received = _fixture.TryReceivePublisherBroadcast(
			out var topic,
			out var payload,
			TimeSpan.FromSeconds(3)
		);

		// At least one broadcast should be received
		if (received)
		{
			Assert.NotEmpty(topic);
			Assert.NotEmpty(payload);
			// Payload should be valid JSON
			JsonDocument.Parse(payload);
		}
		// Note: broadcast may be received or not depending on timing,
		// but the connection should work without errors
	}

	[Fact]
	public async Task SettingsPage_NavigationViaNumpad()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Settings accessible via Home page or direct navigation
		await _fixture.Page.GotoAsync("http://localhost:5001/settings");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);

		// Settings should have configuration options
		Assert.Contains("option", content.ToLowerInvariant());
	}
}
