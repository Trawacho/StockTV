using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.Pages;

public class HomeBase : ComponentBase, IDisposable
{
	[Inject] protected NavigationManager? NavManager { get; set; }
	[Inject] protected SettingsService? SettingsService { get; set; } // <-- Service injiziert


	protected int countdown = 10;
	protected int progress = 0;

	private Timer? _timer;
	private bool _disposed = false;

	protected override void OnInitialized()
	{

	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (firstRender)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				// Debug-Modus: Countdown auf 3 Sekunden setzen
				countdown = 3;
			}

			int total = countdown;

			// Timer asynchron starten
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
	}


	public void Dispose()
	{
		if (!_disposed)
		{
			_timer?.Dispose();
			_disposed = true;
		}
	}


	private void NavigateToPage()
	{
		if(SettingsService == null || NavManager == null)
		{
			return;
		}

		var settings = SettingsService.CurrentSettings;
		string _pageName = settings.Modus switch
		{
			Models.Settings.MODUS.TRAINING => ("/training"),
			Models.Settings.MODUS.TURNIER => ("/turnier"),
			Models.Settings.MODUS.BESTOF => ("/bestof"),
			Models.Settings.MODUS.ZIEL => ("/ziel"),
			_ => ("/settings"),
		};


		//Hack: Für Tests hier die entsprechende Seite hart codieren, damit die Navigation funktioniert, ohne dass die SettingsService-Logik berücksichtigt werden muss.
		//pageName = "layouttest";

		if (!string.IsNullOrEmpty(_pageName))
		{
			NavManager.NavigateTo(_pageName);
		}

	}
}