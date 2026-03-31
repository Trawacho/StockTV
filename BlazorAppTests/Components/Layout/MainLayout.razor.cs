using Microsoft.AspNetCore.Components;

namespace BlazorAppTests.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] protected NavigationManager Nav { get; set; } = default!;

        protected override void OnInitialized()
        {
            // NavigationManager ist hier verfügbar
            //Nav.NavigateTo("/layouttest1", replace: true);
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Nav.NavigateTo("/layouttest1", replace: true);
            }
        }
    }
}