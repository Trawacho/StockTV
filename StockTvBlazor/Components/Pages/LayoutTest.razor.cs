using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using StockTvBlazor.Components.Pages;
using StockTvBlazor.Components.Services;
using System.Threading.Tasks;

namespace BlazorAppTests.Components.Pages
{
    public class LayoutTestBase : ComponentBase
    {

        [Inject] protected NavigationManager? NavManager { get; set; }
        [Inject] protected SettingsService? SettingsService { get; set; }

        [Inject] protected StockTvBlazor.Components.ViewModels.TrainingViewModel ViewModel { get; set; }
        protected PunkteEingabe? punkteEingabeRef;

        // Sichtbarkeit der Panels und Werbung
        protected bool isAdvertVisible = false;
        protected bool isLeftPanelVisible = true;
        protected bool isRightPanelVisible = true;

        // Beispielwerte für Punkte und Eingaben
        protected int scoreA = 11;
        protected int scoreB = 2;
        protected string links = "2-0-4-3-2-0";
        protected string rechts = "0-1-0-0-0-1";
        protected string meinWert = "";
        protected int eingabeWert = 0;

        protected string test = "läuft";
        protected ElementReference inputRef;

        // Lifecycle: Setzt Fokus auf PunkteEingabe
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && punkteEingabeRef != null)
            {
                await punkteEingabeRef.SetFocusAsync();
            }
        }

        // Methoden
        protected void HandleWertChanged(string wert)
        {
            meinWert = wert;
        }

        protected void ToggleLine4()
        {
            isAdvertVisible = !isAdvertVisible;
        }

        protected void OnKeyDownGlobal(KeyboardEventArgs e)
        {
            test = $"Global: {e.Key}";
        }

        protected async Task HandleGlobalKeyDown(KeyboardEventArgs e)
        {
            await ViewModel.ProcessKeyAsync(e.Key);
        }
    }
}