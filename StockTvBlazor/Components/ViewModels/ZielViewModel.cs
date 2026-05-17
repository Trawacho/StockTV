using StockTvBlazor.Services;
using StockTvBlazor.Models;

namespace StockTvBlazor.Components.ViewModels;

public class ZielViewModel : IDisposable
{
	public ZielViewModel(
		ZielService zielService,
		SettingsService settingsService)
	{
		_zielService = zielService;
		_settingsService = settingsService;

		_currentBewerb = _zielService.CurrentZielBewerb;
		_currentBewerb.OnZielBewerbChanged += HandleZielBewerbChanged;
		_zielService.OnGlobalRefresh += HandleZielBewerbChanged;
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		_currentBewerb.OnZielBewerbChanged -= HandleZielBewerbChanged;
		_zielService.OnGlobalRefresh -= HandleZielBewerbChanged;
	}

	private void HandleZielBewerbChanged() => OnViewModelChanged?.Invoke();

	public event Action? OnViewModelChanged;

	private bool _disposed;
	private readonly ZielService _zielService;
	private readonly SettingsService _settingsService;
	private readonly ZielBewerb _currentBewerb;

	public string GesamtText => "Gesamt:";
	public string SpielerName => _currentBewerb.Spielername;

	public bool IsSpielernameAvailable => !string.IsNullOrEmpty(SpielerName);

	public string GesamtPunkteText => _currentBewerb.GesamtSumme.ToString();

	public string AnzahlVersuche
	{
		get
		{
			var s = _settingsService.CurrentSettings;
			return $"{_currentBewerb.AnzahlVersuche()}/{s.Game.MaxKehrenProSpiel * 4}";
		}
	}

	public string SummeDerVersuche =>
		$"{_currentBewerb.MassenVorneSumme} - {_currentBewerb.SchiessenSumme} - {_currentBewerb.MassenSeiteSumme} - {_currentBewerb.KombinierenSumme}";

	public string InputValue => _zielService.InputValue < 0 ? "" : _zielService.InputValue.ToString();

	public string LastValue => _zielService.InputValue == -1
		? _currentBewerb.AnzahlVersuche() > 0
			? _currentBewerb.LetzterVersuch().ToString()
			: string.Empty
		: string.Empty;

	public bool InvalidInput => _zielService.InvalidInput;
}
