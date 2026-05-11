using NetMQ;
using StockTvBlazor.Components;
using StockTvBlazor.Components.ViewModels;
using StockTvBlazor.Networking;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;


//GLOBAL EXCEPTION HANDLER
void LogError(string title, object ex)
{
    var oldColor = Console.ForegroundColor;

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("=================================");
    Console.WriteLine(title);
    Console.WriteLine(ex);
    Console.WriteLine("=================================");

    Console.ForegroundColor = oldColor;
}

AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    LogError("UNHANDLED EXCEPTION", e.ExceptionObject);
};

TaskScheduler.UnobservedTaskException += (sender, e) =>
{
    LogError("TASK ERROR", e.Exception);
    e.SetObserved();
};

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFileLogger();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<SettingsService>());
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

// WICHTIG: Development richtig behandeln!
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

NetMQConfig.Cleanup();