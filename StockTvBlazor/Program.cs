using NetMQ;
using StockTvBlazor.Components;
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
builder.Services.AddSingleton<ZielService>();

builder.Services.AddHostedService<MdnsDiscoveryService>();

builder.Services.AddSingleton<NetMqPublisherService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NetMqPublisherService>());

builder.Services.AddSingleton<NetMqResponseService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<NetMqResponseService>());

builder.Services.AddTransient<TurnierViewModel>();
builder.Services.AddTransient<TrainingViewModel>();
builder.Services.AddTransient<BestOfViewModel>();
builder.Services.AddTransient<SettingsViewModel>();
builder.Services.AddTransient<ZielViewModel>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;

	var settingsService = services.GetRequiredService<SettingsService>();
	await settingsService.InitializeAsync();

	var matchService = services.GetRequiredService<MatchService>();
	matchService.InitializeMatch();

	var zielService = services.GetRequiredService<ZielService>();
	zielService.InitializeZiel();
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

app.UseStaticFiles();		// <== scheinbar notwendig wegen scoped css im DockerContainer
app.MapStaticAssets();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

NetMQConfig.Cleanup();