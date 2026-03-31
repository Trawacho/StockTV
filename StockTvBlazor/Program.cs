using StockTvBlazor.Components;
using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;
using StockTvBlazor.Components.ViewModels;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFileLogger();

// Add services to the container.
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddHostedService<MdnsDiscoveryService>();

builder.Services.AddSingleton<NetMqPublisherService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NetMqPublisherService>());

builder.Services.AddSingleton<NetMqResponseService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NetMqResponseService>());

builder.Services.AddScoped<TurnierViewModel>();
builder.Services.AddScoped<TrainingViewModel>();
builder.Services.AddScoped<BestOfViewModel>();
builder.Services.AddScoped<SettingsViewModel>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;

	var settingsService = services.GetRequiredService<SettingsService>();
	await settingsService.InitializeAsync();

	var matchService = services.GetRequiredService<MatchService>();
	matchService.InitializeMatch();
}

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

app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();
