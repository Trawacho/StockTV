using StockTvBlazor.E2ETests.Fixtures;

namespace StockTvBlazor.E2ETests.Tests;

[Collection("App")]
public class TrainingPageTests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public TrainingPageTests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task TrainingPage_LoadsSuccessfully()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var bodyContent = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(bodyContent);
	}

	[Fact]
	public async Task TrainingPage_ContainsScoreDisplay()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		// Check for score display elements (class names from Training.razor)
		Assert.Contains("score", content.ToLowerInvariant());
	}

	[Fact]
	public async Task TrainingPage_Has_PunkteeingabePassiv_Component()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/training");
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		var content = await _fixture.Page.ContentAsync();
		// PunkteeingabePassiv should render a table
		Assert.Contains("punkte-table", content);
	}
}
