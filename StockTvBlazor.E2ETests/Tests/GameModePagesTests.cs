using StockTvBlazor.E2ETests.Fixtures;

namespace StockTvBlazor.E2ETests.Tests;

/// <summary>
/// Golden-path E2E tests for game mode pages: BestOf, Turnier, Ziel.
/// Tests primarily verify page load and basic component rendering.
/// Functional tests (score input, mode logic) deferred to integration with mocked services.
/// </summary>
[Collection("App")]
public class GameModePagesTests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public GameModePagesTests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task BestOfPage_LoadsWithoutErrors()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/bestof");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		// BestOf should have score display
		Assert.Contains("punkte", content.ToLowerInvariant());
	}

	[Fact]
	public async Task TurnierPage_LoadsWithoutErrors()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/turnier");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		// Turnier should display team names
		Assert.Contains("team", content.ToLowerInvariant());
	}

	[Fact]
	public async Task ZielPage_LoadsWithoutErrors()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/ziel");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		// Ziel should have discipline names or score display
		Assert.Contains("punkte", content.ToLowerInvariant());
	}

	[Fact]
	public async Task InputPage_LoadsWithIframe()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/input");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		// Input page should contain iframe
		Assert.Contains("iframe", content.ToLowerInvariant());
	}

	[Fact]
	public async Task SettingsPage_LoadsWithFormElements()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/settings");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(content);
		// Settings should have various form elements or options
		Assert.Contains("option", content.ToLowerInvariant());
	}
}
