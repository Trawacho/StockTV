using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Services;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorAppTests.Components.Pages
{
    public class HomeBase : ComponentBase, IDisposable
    {
        [Inject] protected NavigationManager NavManager { get; set; }
        [Inject] protected SettingsService _settingsService { get; set; } // <-- Service injiziert
        protected string mySetting;


        protected int countdown = 10;
        protected int progress = 0;

        private Timer timer;
        private bool disposed = false;

        protected override void OnInitialized()
        {

            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Debug-Modus: Countdown auf 3 Sekunden setzen
                countdown = 3;
            }
            int total = countdown;

            timer = new Timer(async _ =>
            {
                if (countdown > 0)
                {
                    countdown--;
                    progress = (int)((1 - (double)countdown / total) * 100);
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    timer.Dispose();
                    //NavManager.NavigateTo("/layouttest1");
                    NavigateToPage();
                }
            }, null, 1000, 1000);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await Task.Delay(10);
            }
        }


        public void Dispose()
        {
            if (!disposed)
            {
                timer?.Dispose();
                disposed = true;
            }
        }


        private void NavigateToPage()
        {
            var settings = _settingsService.CurrentSettings;
            string pageName = string.Empty;


            switch (settings.Modus)
            {
                case Settings.MODUS.TRAINING:
                    pageName = ("/training");
                    break;
                case Settings.MODUS.TURNIER:
                    pageName = ("/turnier");
                    break;
                case Settings.MODUS.BESTOF:
                    pageName = ("/bestof");
                    break;
                default:
                    pageName = ("/settings");
                    break;
            }


            //Hack: Für Tests hier die entsprechende Seite hart codieren, damit die Navigation funktioniert, ohne dass die SettingsService-Logik berücksichtigt werden muss.
            pageName = "layouttest1";

            if (!string.IsNullOrEmpty(pageName))
            {
                NavManager.NavigateTo(pageName);
            }   

        }
    }
}