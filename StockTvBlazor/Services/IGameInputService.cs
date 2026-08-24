namespace StockTvBlazor.Services;

// Gemeinsame Schnittstelle von MatchService und ZielService für die
// Spielseiten-Basisklasse (Components/Pages/MirrorableGamePageBase.cs).
public interface IGameInputService
{
	event Action? OnGlobalRefresh;

	event Action<string>? OnNavigationRequested;

	Task ProcessKeyAsync(string value);
}
