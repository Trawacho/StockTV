using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StockTvBlazor.Services;

namespace StockTvBlazor.Components.Pages;

public class HomeBase : ComponentBase, IAsyncDisposable
{
    [Inject] protected NavigationManager? NavManager { get; set; }
    [Inject] protected SettingsService? SettingsService { get; set; }
    [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] protected MatchService? MatchService { get; set; }

    protected int countdown = 10;
    protected int progress = 0;
    protected int currentCardIndex = 0;

    private CancellationTokenSource _cts = new();
    private Task? _countdownTask;
    private Task? _cardTask;

    protected List<Type> CardComponents = new()
    {
        typeof(HomeCards.CardDisplay1),
        typeof(HomeCards.CardDisplay2),
        typeof(HomeCards.CardDisplay3),
        typeof(HomeCards.CardDisplay4)
    };

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Debugger.IsAttached)
                countdown = 3;

            _countdownTask = RunCountdownAsync(_cts.Token);
            _cardTask = RunCardRotationAsync(_cts.Token);
        }

        return Task.CompletedTask;
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        try
        {
            int total = countdown;

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync(token))
            {
                if (countdown > 0)
                {
                    countdown--;
                    progress = (int)((1 - (double)countdown / total) * 100);

                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    await NavigateToConfiguredPageAsync();
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"COUNTDOWN ERROR: {ex}");
        }
    }

    private async Task RunCardRotationAsync(CancellationToken token)
    {
        try
        {
            if (CardComponents.Count == 0)
                return;

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));

            while (await timer.WaitForNextTickAsync(token))
            {
                currentCardIndex++;

                if (currentCardIndex >= CardComponents.Count)
                    currentCardIndex = 0;

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CARD ROTATION ERROR: {ex}");
        }
    }

    private async Task NavigateToConfiguredPageAsync()
    {
        try
        {
            if (SettingsService == null || NavManager == null)
                return;

            var settings = SettingsService.CurrentSettings;
            string pageName = SettingsService.GetModusUrl(settings.Game.CurrentModus);
            string[] pagesToOpen = [pageName];

            if (Debugger.IsAttached)
            {
                pagesToOpen = new[] { "LayoutTest", "training", "turnier" , "bestof", "input", "settings", "themes"};
                var sampleNames = System.Text.Encoding.UTF8.GetBytes("1:ESF Hankofen:SV Pilgramsberg;2:EC Neubänrdorf Regen:EC Moitzerlitz Regen;");
                MatchService?.SetTeamNames(sampleNames);
            }

            if (pagesToOpen.Length == 1)
            {
                await InvokeAsync(() => NavManager.NavigateTo(pagesToOpen[0]));
            }
            else
            {
                foreach (var page in pagesToOpen)
                {
                    try
                    {
                        await JSRuntime.InvokeVoidAsync("open", page, "_blank");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"JS ERROR: {ex}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NAVIGATION ERROR: {ex}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();

            if (_countdownTask != null)
                await _countdownTask;

            if (_cardTask != null)
                await _cardTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DISPOSE ERROR: {ex}");
        }
        finally
        {
            _cts.Dispose();
        }
    }
}