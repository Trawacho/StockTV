using StockTvBlazor.E2ETests.Fixtures;

namespace StockTvBlazor.E2ETests.Tests;

[Collection("App")]
public class HomePageTests : IAsyncLifetime
{
	private readonly AppFixture _fixture;

	public HomePageTests()
	{
		_fixture = new AppFixture();
	}

	public Task InitializeAsync() => _fixture.InitializeAsync();
	public Task DisposeAsync() => _fixture.DisposeAsync();

	[Fact]
	public async Task HomePage_LoadsSuccessfully()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/");

		var title = await _fixture.Page.TitleAsync();
		Assert.NotEmpty(title);
	}

	[Fact]
	public async Task HomePage_ContainsHeadingOrContent()
	{
		if (_fixture.Page == null)
			throw new InvalidOperationException("Page not initialized");

		await _fixture.Page.GotoAsync("http://localhost:5001/");

		// Wait for page to be interactive
		await _fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

		// Check that page has some content
		var bodyContent = await _fixture.Page.ContentAsync();
		Assert.NotEmpty(bodyContent);
	}
}
