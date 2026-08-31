using NetMQ;
using StockTvBlazor.Api;
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

builder.Host.UseWindowsService();

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
builder.Services.AddSingleton<FontService>();
builder.Services.AddSingleton<PlatformInfoService>();
builder.Services.AddSingleton<NetworkConfigService>();
builder.Services.AddSingleton<UpdateService>();
builder.Services.AddSingleton<MarketingImageService>();
builder.Services.AddSingleton<SubscriberRegistry>();
builder.Services.AddSingleton<GameEventBroadcaster>();
builder.Services.AddSingleton<GameCommandQueue>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GameCommandQueue>());
builder.Services.AddSingleton<GameStateStore>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GameStateStore>());

builder.Services.AddHttpClient("GitHub", c =>
{
	c.DefaultRequestHeaders.UserAgent.ParseAdd("StockTV-UpdateChecker");
	c.Timeout = TimeSpan.FromSeconds(10);
});

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

builder.Services.Configure<HostOptions>(options =>
{
	options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});




var app = builder.Build();

// Eigener Web-Host der REST-Schnittstelle; bleibt null, wenn sie abgeschaltet ist oder nicht
// starten konnte (siehe StockTvApiHost.StartIfEnabledAsync).
StockTvApiHost? apiHost = null;

app.Lifetime.ApplicationStopping.Register(() =>
{
	// Vor dem harten Exit unten sauber beenden, damit der Port wieder frei wird.
	if (apiHost is not null)
		apiHost.DisposeAsync().AsTask().GetAwaiter().GetResult();

	Task.Run(async () =>
	{
		await Task.Delay(5000);
		Environment.Exit(0);
	});
});

using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;

	var settingsService = services.GetRequiredService<SettingsService>();
	await settingsService.InitializeAsync();

	var matchService = services.GetRequiredService<MatchService>();
	matchService.InitializeMatch();

	var zielService = services.GetRequiredService<ZielService>();
	zielService.InitializeZiel();

	// Nach dem Laden der Konfiguration: die Gueltigkeitspruefung des Spielstands braucht
	// Bahnnummer und Modus. Ein nicht passender oder zu alter Stand wird dabei verworfen.
	var gameStateStore = services.GetRequiredService<GameStateStore>();
	var savedState = await gameStateStore.LoadAsync(settingsService.CurrentSettings);

	if (savedState is not null)
	{
		gameStateStore.RestoreWithoutSaving(() =>
		{
			matchService.CurrentMatch.RestoreFrom(savedState.Match);
			zielService.CurrentZielBewerb.RestoreFrom(savedState.Ziel);
		});
	}

	// Erst nach dem Wiederherstellen anmelden, sonst schreibt das Wiederherstellen selbst.
	gameStateStore.StartAutoSave(() => new GameStateFile
	{
		SavedAtUtc = DateTimeOffset.UtcNow,
		BahnNummer = settingsService.CurrentSettings.General.BahnNummer,
		Modus = settingsService.CurrentSettings.Game.CurrentModus,
		Match = matchService.CurrentMatch.CreateSnapshot(),
		Ziel = zielService.CurrentZielBewerb.CreateSnapshot()
	});

	// Jede Aenderung an Spielstand oder Zielbewerb sichern. Ueber die Ereignisse der Modelle und
	// nicht an den einzelnen Eingabestellen: so ist auch erfasst, was von aussen kommt
	// (SetTeamNames, SetTeilnehmer, ResetResult ueber NetMQ).
	matchService.CurrentMatch.OnMatchChanged += gameStateStore.RequestSave;
	zielService.CurrentZielBewerb.OnZielBewerbChanged += gameStateStore.RequestSave;

	// Erst hier, nach InitializeAsync(): der eigene Web-Host der REST-Schnittstelle braucht die
	// geladene Konfiguration (Port, BindAddress, ApiKey). Genau deshalb ein zweiter Host und
	// kein zweiter Listener im Anzeige-Host - dessen Kestrel steht schon vor builder.Build().
	apiHost = await StockTvApiHost.StartIfEnabledAsync(
		settingsService.CurrentSettings,
		app.Services,
		app.Services.GetRequiredService<ILoggerFactory>());
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

// Liefert das Werbebild aus. Bewusst am Anzeige-Host (8080) und nicht an der REST-Schnittstelle:
// die Seite /marketing laeuft im Browser des Kiosks, der die API auf 8098 weder erreichen soll
// noch den API-Schluessel kennt.
app.MapGet("/marketing/image", (MarketingImageService images) =>
{
	var file = images.CurrentFile;

	return file is null
		? Results.NotFound()
		: Results.File(file, MarketingImageService.ContentTypeFor(file));
});

app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode();

app.Run();

NetMQConfig.Cleanup();
