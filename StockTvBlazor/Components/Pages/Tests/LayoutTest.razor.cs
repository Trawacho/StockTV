using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using StockTvBlazor.Services;

namespace BlazorAppTests.Components.Pages
{
    public class LayoutTestBase : ComponentBase
    {
        [Inject] protected NavigationManager? NavManager { get; set; }
        [Inject] protected SettingsService? SettingsService { get; set; }
        [Inject] protected MatchService MatchService { get; set; } = default!;
        [Inject] protected StockTvBlazor.Components.ViewModels.TrainingViewModel ViewModel { get; set; } = default!;

        protected bool isAdvertVisible = false;
        protected bool isLeftPanelVisible = true;
        protected bool isRightPanelVisible = true;

        protected ElementReference inputRef;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // optional: noch stabiler
                await Task.Yield();
                await inputRef.FocusAsync();
            }
        }

        protected void ToggleLine4()
        {
            isAdvertVisible = !isAdvertVisible;
        }

        protected async Task HandleGlobalKeyDown(KeyboardEventArgs e)
        {
            await MatchService.ProcessKeyAsync(e.Key);
        }
    }
}