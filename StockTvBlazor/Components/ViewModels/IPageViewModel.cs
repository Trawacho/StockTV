namespace StockTvBlazor.Components.ViewModels;

// Gemeinsame Schnittstelle von BaseViewModel und ZielViewModel für die
// Spielseiten-Basisklasse (Components/Pages/MirrorableGamePageBase.cs).
public interface IPageViewModel : IDisposable
{
	event Action? OnViewModelChanged;

	void EnableDemoMode();
}
