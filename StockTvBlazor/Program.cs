using StockTvBlazor.Components;
using StockTvBlazor.Components.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<Settings>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Server-side decision: redirect root ('/') to the configured start page from Settings.
app.MapGet("/", (context) =>
{
    var settings = context.RequestServices.GetRequiredService<Settings>();
    var target = settings.StartPage ?? "/training";
    context.Response.Redirect(target);
    return System.Threading.Tasks.Task.CompletedTask;
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
