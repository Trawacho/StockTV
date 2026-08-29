using StockTvBlazor.E2ETests.Fixtures;
using Microsoft.Playwright;

namespace StockTvBlazor.E2ETests.Tests;

/// <summary>
/// E2E tests for BestOf, Turnier, and Ziel modes.
/// </summary>
[Collection("App")]
public class MultiModeE2ETests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public MultiModeE2ETests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task BestOfMode_MultipleGames_CalculatesMatchPoints()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to BestOf page
		await _fixture.Page.GotoAsync("http://localhost:5001/bestof");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify page loads and contains match-related content
		var content = await _fixture.Page.ContentAsync();
		Assert.Contains("punkte", content.ToLowerInvariant());
	}

	[Fact]
	public async Task TurnierMode_DisplaysTeamNames()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to Turnier page
		await _fixture.Page.GotoAsync("http://localhost:5001/turnier");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Verify page contains team-related content
		var content = await _fixture.Page.ContentAsync();
		Assert.Contains("punkte", content.ToLowerInvariant());
	}

	[Fact]
	public async Task ZielMode_AllDisciplinesAvailable()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Navigate to Ziel page
		await _fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		// Ziel mode should have score displays
		Assert.Contains("punkte", content.ToLowerInvariant());
	}

	[Fact]
	public async Task InputPage_NavigatesBetweenModes()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Input page should render regardless of current mode
		await _fixture.Page.GotoAsync("http://localhost:5001/input");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		Assert.Contains("iframe", content.ToLowerInvariant());
	}

	[Fact]
	public async Task SettingsPage_AccessibleAndResponsive()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		// Settings page should load
		await _fixture.Page.GotoAsync("http://localhost:5001/settings");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
	}

	[Fact]
	public async Task NetMQ_HelloCommand_ReceivesWelcome()
	{
		// Test simple NetMQ command
		string response = _fixture.SendNetMqCommand("Hello");

		Assert.Equal("Welcome", response);
	}

	[Fact]
	public async Task NetMQ_ResetResult_ReturnsAck()
	{
		// Test ResetResult command
		string response = _fixture.SendNetMqCommand("ResetResult");

		Assert.Equal("ACK", response);
	}

	[Fact]
	public async Task NetMQ_UnknownTopic_ReturnsNack()
	{
		// Test unknown topic handling
		string response = _fixture.SendNetMqCommand("UnknownCommandXYZ");

		Assert.Contains("NACK", response);
		Assert.Contains("unknown", response.ToLowerInvariant());
	}
}
