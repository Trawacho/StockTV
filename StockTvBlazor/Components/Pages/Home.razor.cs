using Microsoft.AspNetCore.Components;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorAppTests.Components.Pages
{
    public class HomeBase : ComponentBase, IDisposable
    {
        [Inject] protected NavigationManager NavManager { get; set; }

        protected int countdown = 10;
        protected int progress = 0;

        private Timer timer;
        private bool disposed = false;

        protected override void OnInitialized()
        {
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
                    NavManager.NavigateTo("/layouttest1");
                }
            }, null, 1000, 1000);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                timer?.Dispose();
                disposed = true;
            }
        }
    }
}