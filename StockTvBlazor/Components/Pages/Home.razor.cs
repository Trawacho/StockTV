using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.Pages;

public class HomeBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationManager? NavManager { get; set; }
    [Inject] protected SettingsService? SettingsService { get; set; }

    protected int countdown = 10;
    protected int progress = 0;

    private Timer? _timer;
    private Timer? _cardTimer;
    private bool _disposed = false;

    protected int currentCardIndex = 0;

    // Liste deiner Card-Komponenten
    protected List<Type> CardComponents = new()
    {
        typeof(HomeCards.CardDisplay1),
        typeof(HomeCards.CardDisplay2),
        typeof(HomeCards.CardDisplay3),
        typeof(HomeCards.CardDisplay4)
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            //if (System.Diagnostics.Debugger.IsAttached) { countdown = 3; }

            StartCountdown();
            StartCardRotation();
        }
    }

    private void StartCountdown()
    {
        int total = countdown;

        _timer = new Timer(async _ =>
        {
            if (countdown > 0)
            {
                countdown--;
                progress = (int)((1 - (double)countdown / total) * 100);
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                _timer?.Dispose();
                NavigateToPage();
            }

        }, null, 1000, 1000);
    }

    private void StartCardRotation()
    {
        _cardTimer = new Timer(async _ =>
        {
            currentCardIndex++;

            if (currentCardIndex >= CardComponents.Count)
                currentCardIndex = 0; // Loop

            await InvokeAsync(StateHasChanged);

        }, null, 2000, 2000);
    }

    private void NavigateToPage()
    {
        if (SettingsService == null || NavManager == null)
            return;

        var settings = SettingsService.CurrentSettings;
        string pageName = SettingsService.GetModusUrl(settings.Modus);

        if (!string.IsNullOrEmpty(pageName))
            NavManager.NavigateTo(pageName);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _timer?.Dispose();
        _cardTimer?.Dispose();

        _disposed = true;
    }
}
