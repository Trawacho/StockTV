using StockTvBlazor.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddHostedService<MdnsDiscoveryService>();

builder.Services.AddSingleton<NetMqPublisherService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NetMqPublisherService>());


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
	var _settings = context.RequestServices.GetRequiredService<SettingsService>().CurrentSettings;
	switch (_settings.Modus)
	{
		case Settings.MODUS.TRAINING:
			context.Response.Redirect("/training");
			break;
		case Settings.MODUS.TURNIER:
			context.Response.Redirect("/turnier");
			break;
		case Settings.MODUS.BESTOF:
			context.Response.Redirect("/bestof");
			break;
		default:
			context.Response.Redirect("/settings");
			break;
	}
	return System.Threading.Tasks.Task.CompletedTask;
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
