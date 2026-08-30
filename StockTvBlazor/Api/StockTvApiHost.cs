using System.Net;
using Microsoft.OpenApi;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Api;

/// <summary>
/// Startet die REST-Schnittstelle auf einem eigenen Kestrel-Host, getrennt von der
/// Blazor-Anzeige.
/// </summary>
/// <remarks>
/// Eigener Host statt eines zweiten Listeners im Anzeige-Host, weil in einer WebApplication
/// alle Endpunkte auf allen Listenern antworten - die Schnittstelle waere sonst auch ueber
/// Port 8080 erreichbar, also genau die Luecke, die der eigene Port schliessen soll. Ausserdem
/// teilte sie sich dann die Middleware-Kette der Anzeige (HttpsRedirection, Antiforgery,
/// StatusCodePagesWithReExecute) und ein belegter Port 8099 wuerde die Anzeige mit
/// herunterreissen.
///
/// Die Dienste werden als fertige Instanzen aus dem Haupt-Container uebergeben, nicht neu
/// erzeugt: MatchService, ZielService und SettingsService halten den Spielstand, davon darf es
/// nur einen geben. Muster uebernommen aus StockTvKiosk (KioskApiHost).
/// </remarks>
public sealed class StockTvApiHost(WebApplication app, ILogger logger) : IAsyncDisposable
{
	/// <summary>
	/// Baut und startet die Schnittstelle, sofern eingeschaltet und zulaessig konfiguriert.
	/// Gibt null zurueck, wenn sie nicht laufen soll - die Anzeige laeuft dann normal weiter.
	/// </summary>
	public static async Task<StockTvApiHost?> StartIfEnabledAsync(
		Settings.Settings settings,
		IServiceProvider mainServices,
		ILoggerFactory loggerFactory)
	{
		var logger = loggerFactory.CreateLogger("StockTvBlazor.Api");
		var rest = settings.RestApi;

		if (!rest.Enabled)
		{
			logger.LogInformation("Die REST-Schnittstelle ist abgeschaltet (RestApi > Enabled = false).");
			return null;
		}

		if (!TryResolveAddress(rest.BindAddress, out var address))
		{
			logger.LogError(
				"RestApi > BindAddress ist keine gueltige Adresse: {Address}. " +
				"Die Schnittstelle wird nicht gestartet.", rest.BindAddress);
			return null;
		}

		// Ohne Schluessel koennte jeder im Netz Spielstaende ueberschreiben oder das Geraet neu
		// starten. Auf Loopback kommt ohnehin nur heran, wer schon am Rechner ist.
		bool loopbackOnly = IPAddress.IsLoopback(address);
		if (!loopbackOnly && string.IsNullOrEmpty(rest.ApiKey))
		{
			logger.LogError(
				"Die REST-Schnittstelle soll auf {Address} lauschen, aber RestApi > ApiKey ist leer. " +
				"Ohne Schluessel koennte jeder im Netz das Geraet fernsteuern - die Schnittstelle " +
				"wird deshalb nicht gestartet. Entweder einen Schluessel in " +
				"_config/stocktv.config.json eintragen (er wird beim naechsten Speichern " +
				"verschluesselt abgelegt) oder BindAddress auf 127.0.0.1 setzen.",
				rest.BindAddress);
			return null;
		}

		var app = Build(settings, mainServices, loggerFactory, address);

		try
		{
			await app.StartAsync();
		}
		catch (Exception ex)
		{
			// Ein belegter Port darf nicht die ganze Anzeige verhindern - die Punkteanzeige ist
			// wichtiger als die Fernsteuerung. Genau dafuer laeuft die Schnittstelle in einem
			// eigenen Host.
			logger.LogError(ex,
				"Die REST-Schnittstelle konnte nicht auf {Address}:{Port} starten. " +
				"Die Anzeige laeuft ohne sie weiter.", rest.BindAddress, rest.Port);

			await app.DisposeAsync();
			return null;
		}

		logger.LogInformation("REST-Schnittstelle laeuft auf http://{Address}:{Port}{Auth}",
			rest.BindAddress, rest.Port,
			loopbackOnly && string.IsNullOrEmpty(rest.ApiKey) ? " (ohne Schluessel, nur lokal)" : "");

		if (rest.SwaggerEnabled)
		{
			logger.LogInformation("Swagger: http://{Address}:{Port}/{Route}",
				rest.BindAddress, rest.Port, rest.SwaggerRoute.Trim('/'));
		}

		return new StockTvApiHost(app, logger);
	}

	private static WebApplication Build(
		Settings.Settings settings,
		IServiceProvider mainServices,
		ILoggerFactory loggerFactory,
		IPAddress address)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [] });

		// Die einzige Konfigurationsquelle ist stocktv.config.json. CreateBuilder bringt von
		// sich aus appsettings.json, Umgebungsvariablen und Kommandozeile mit - ein gesetztes
		// ASPNETCORE_URLS wuerde hier still gegen RestApi > Port arbeiten.
		builder.Configuration.Sources.Clear();

		builder.WebHost.ConfigureKestrel(kestrel =>
		{
			kestrel.Listen(address, settings.RestApi.Port);

			// Das Geraet nennt sich nicht selbst in jeder Antwort.
			kestrel.AddServerHeader = false;
		});

		// Ein einziges Log fuer Anzeige und Schnittstelle, in derselben Datei.
		builder.Logging.ClearProviders();
		builder.Services.AddSingleton(loggerFactory);

		// Dieselben Instanzen wie die Anzeige - kein zweiter Spielstand.
		builder.Services.AddSingleton(settings);
		builder.Services.AddSingleton(settings.RestApi);
		builder.Services.AddSingleton(mainServices.GetRequiredService<SettingsService>());
		builder.Services.AddSingleton(mainServices.GetRequiredService<MatchService>());
		builder.Services.AddSingleton(mainServices.GetRequiredService<ZielService>());
		builder.Services.AddSingleton(mainServices.GetRequiredService<PlatformInfoService>());
		builder.Services.AddSingleton(mainServices.GetRequiredService<NetworkConfigService>());

		builder.Services.AddControllers();

		if (settings.RestApi.SwaggerEnabled)
			AddSwagger(builder, settings.RestApi);

		var app = builder.Build();

		app.UseMiddleware<ApiKeyMiddleware>(settings.RestApi);

		if (settings.RestApi.SwaggerEnabled)
		{
			app.UseSwagger();
			app.UseSwaggerUI(ui =>
			{
				ui.SwaggerEndpoint("/swagger/v1/swagger.json", "StockTV v1");
				ui.RoutePrefix = settings.RestApi.SwaggerRoute.Trim('/');
			});
		}

		app.MapControllers();
		return app;
	}

	private static void AddSwagger(WebApplicationBuilder builder, RestApiSettings rest)
	{
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(swagger =>
		{
			swagger.SwaggerDoc("v1", new OpenApiInfo
			{
				Title = "StockTV",
				Version = "v1",
				Description = "Schnittstelle fuer das zentrale Verwaltungsprogramm: Spielstand " +
							  "abrufen, Einstellungen setzen und das Geraet verwalten."
			});

			// Die Beschreibungen der Endpunkte stammen aus den XML-Kommentaren im Quelltext.
			string xml = Path.Combine(AppContext.BaseDirectory,
				$"{typeof(StockTvApiHost).Assembly.GetName().Name}.xml");
			if (File.Exists(xml))
				swagger.IncludeXmlComments(xml);

			if (string.IsNullOrEmpty(rest.ApiKey))
				return;

			// Damit die Oberflaeche ein Eingabefeld fuer den Schluessel anbietet - ohne das
			// liefert dort jeder Aufruf 401 und niemand sieht, warum.
			swagger.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
			{
				Name = rest.ApiKeyHeader,
				Type = SecuritySchemeType.ApiKey,
				In = ParameterLocation.Header,
				Description = $"Der Wert aus RestApi > ApiKey, als Kopfzeile {rest.ApiKeyHeader}."
			});

			swagger.AddSecurityRequirement(document => new OpenApiSecurityRequirement
			{
				[new OpenApiSecuritySchemeReference("ApiKey", document)] = []
			});
		});
	}

	/// <summary>Uebersetzt die konfigurierte Adresse; akzeptiert auch "localhost" und "*".</summary>
	internal static bool TryResolveAddress(string? configured, out IPAddress address)
	{
		string value = (configured ?? string.Empty).Trim();

		switch (value.ToLowerInvariant())
		{
			case "":
			case "localhost":
				address = IPAddress.Loopback;
				return true;
			case "*":
			case "+":
			case "any":
				address = IPAddress.Any;
				return true;
			default:
				return IPAddress.TryParse(value, out address!);
		}
	}

	public async ValueTask DisposeAsync()
	{
		try
		{
			using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			await app.StopAsync(timeout.Token);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Die REST-Schnittstelle liess sich nicht sauber beenden.");
		}

		await app.DisposeAsync();
	}
}
