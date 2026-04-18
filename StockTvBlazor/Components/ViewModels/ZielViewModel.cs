using StockTvBlazor.Services;
using StockTvBlazor.Models;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Components.ViewModels;

public class ZielViewModel : IDisposable
{
    public ZielViewModel(
        ZielService zielService,
        SettingsService settingsService,
        NetMqPublisherService publisherService)
    {
        _zielService = zielService;
        _settingsService = settingsService;
        _publisherService = publisherService;

        _currentBewerb = _zielService.CurrentZielBewerb;
        _currentBewerb.OnZielBewerbChanged += HandleZielBewerbChanged;
    }

    public event Action<string>? OnNavigationRequested;
    public event Action? OnViewModelChanged;

    private bool _disposed;
    private readonly ZielService _zielService;
    private readonly SettingsService _settingsService;
    private readonly NetMqPublisherService _publisherService;
    private readonly ZielBewerb _currentBewerb;

    public string GesamtText => "Gesamt:";
    public string SpielerName => _currentBewerb.Spielername;

    private int _inputValue;
    public int _specialCounter;
    private readonly Debounce _debounce = new();

    private void HandleZielBewerbChanged()
    {
        OnViewModelChanged?.Invoke();
    }

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

    public string InputValue => _inputValue < 0 ? "" : _inputValue.ToString();

    public string LastValue => _inputValue == -1
        ? _currentBewerb.AnzahlVersuche() > 0
            ? _currentBewerb.LetzterVersuch().ToString()
            : string.Empty
        : string.Empty;

    private bool _invalidInput;
    public bool InvalidInput
    {
        get => _invalidInput;
        set
        {
            if (_invalidInput != value)
            {
                _invalidInput = value;
                OnViewModelChanged?.Invoke();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _currentBewerb.OnZielBewerbChanged -= HandleZielBewerbChanged;
    }

    public async Task ProcessKeyAsync(string value)
    {
        var s = _settingsService.CurrentSettings;

        if ((_inputValue == 0 || _inputValue == 10)
            && value == "Enter"
            && !s.General.BlockLocalChanges)
        {
            _specialCounter++;
        }
        else
        {
            _specialCounter = 0;
        }

        if (!(value == "-" && _inputValue == 0 && !s.General.BlockLocalChanges))
        {
            if (!_debounce.IsDebounceOk(value))
                return;
        }

        switch (value)
        {
            case "Enter": ShowSpecialPage(); break;
            case "/" or "Backspace" or "*": await AddToZielBewerb(); break;
            case "-": await DeleteLastValueAsync(); break;
            case "+": await ResetAsync(); break;

            default:
                int? input = value switch
                {
                    "1" or "End" => 1,
                    "2" or "ArrowDown" => 2,
                    "3" or "PageDown" => 3,
                    "4" or "ArrowLeft" => 4,
                    "5" or "Clear" => 5,
                    "6" or "ArrowRight" => 6,
                    "7" or "Home" => 7,
                    "8" or "ArrowUp" => 8,
                    "9" or "PageUp" => 9,
                    "0" or "Insert" => 0,
                    _ => null
                };

                if (input.HasValue)
                    AddInput(input.Value);

                break;
        }

        OnViewModelChanged?.Invoke();
        _publisherService.Publish("GetResult", _currentBewerb.SerializeJson());
    }

    private void AddInput(int value)
    {
        var s = _settingsService.CurrentSettings;

        int newValue = (_inputValue < 0) ? value : (_inputValue * 10) + value;
        int maxPoints = s.Game.MaxPunkteProKehre;

        if (newValue <= maxPoints)
            _inputValue = newValue;
        else
            _inputValue = (value <= maxPoints) ? value : -1;
    }

    private async Task ResetAsync()
    {
        var s = _settingsService.CurrentSettings;

        if (_currentBewerb.AnzahlVersuche() < s.Game.MaxKehrenProSpiel * 4)
            return;

        _currentBewerb.Reset();
        _inputValue = -1;
    }

    private async Task AddToZielBewerb()
    {
        if (_inputValue < 0) return;

        if (!_currentBewerb.AddVersuch(_inputValue))
        {
            _inputValue = -1;

            InvalidInput = true;
            await Task.Delay(1500);
            InvalidInput = false;
            return;
        }

        _inputValue = -1;
    }

    private async Task DeleteLastValueAsync()
    {
        if (_inputValue >= 0)
            _inputValue = -1;
        else
            _currentBewerb.DeleteLastVersuch();
    }

    private protected void ShowSpecialPage()
    {
        if (_specialCounter < 5) return;

        _specialCounter = 0;

        if (_inputValue == 0)
        {
            OnNavigationRequested?.Invoke("/settings");
        }
        else if (_inputValue == 10)
        {
            // TODO Marketing
        }
    }
}