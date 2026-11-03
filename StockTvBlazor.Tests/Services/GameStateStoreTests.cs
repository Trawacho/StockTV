using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;
using AppSettings = StockTvBlazor.Settings.Settings;

namespace StockTvBlazor.Tests.Services;

/// <summary>
/// Prueft Laden und Speichern des Spielstands (<c>_config/stocktv.state.json</c>), insbesondere
/// die Regel, dass im Modus Training nichts gesichert oder geladen wird.
/// </summary>
/// <remarks>
/// Der Store nutzt einen festen Pfad im Ausgabeverzeichnis. Die Tests laufen deshalb nacheinander
/// in einer Klasse und raeumen die Datei vor und nach jedem Test weg.
/// </remarks>
public class GameStateStoreTests : IDisposable
{
	private const int Bahn = 3;

	public GameStateStoreTests() => DeleteStateFile();

	public void Dispose() => DeleteStateFile();

	private static void DeleteStateFile()
	{
		if (File.Exists(GameStateStore.FilePath))
			File.Delete(GameStateStore.FilePath);
	}

	private static GameStateStore CreateStore() => new(NullLogger<GameStateStore>.Instance);

	private static AppSettings CreateSettings(GameSettings.Modus modus)
	{
		var settings = new AppSettings();
		settings.General.BahnNummer = Bahn;
		settings.Game.CurrentModus = modus;
		return settings;
	}

	private static void WriteStateFile(GameSettings.Modus modus, DateTimeOffset? savedAt = null)
	{
		var state = new GameStateFile
		{
			SavedAtUtc = savedAt ?? DateTimeOffset.UtcNow,
			BahnNummer = Bahn,
			Modus = modus,
			Ziel = new ZielState { MassenVorne = [2, 4] }
		};

		Directory.CreateDirectory(Path.GetDirectoryName(GameStateStore.FilePath)!);
		File.WriteAllText(GameStateStore.FilePath, JsonSerializer.Serialize(state));
	}

	[Fact]
	public async Task Load_PassendeDatei_WirdGeladen()
	{
		WriteStateFile(GameSettings.Modus.Ziel);

		var loaded = await CreateStore().LoadAsync(CreateSettings(GameSettings.Modus.Ziel));

		Assert.NotNull(loaded);
		Assert.Equal([2, 4], loaded.Ziel.MassenVorne);
	}

	[Fact]
	public async Task Load_ImTraining_LiefertNullUndLoeschtDatei()
	{
		WriteStateFile(GameSettings.Modus.Ziel);

		var loaded = await CreateStore().LoadAsync(CreateSettings(GameSettings.Modus.Training));

		Assert.Null(loaded);
		Assert.False(File.Exists(GameStateStore.FilePath));
	}

	[Fact]
	public async Task Load_DateiAusDemTraining_WirdNichtGeladen()
	{
		// Reste einer aelteren Fassung, die auch das Training gesichert hat.
		WriteStateFile(GameSettings.Modus.Training);

		var loaded = await CreateStore().LoadAsync(CreateSettings(GameSettings.Modus.BestOf));

		Assert.Null(loaded);
	}

	[Fact]
	public async Task Load_ZuAlteDatei_WirdNichtGeladen()
	{
		WriteStateFile(GameSettings.Modus.Ziel, DateTimeOffset.UtcNow - GameStateStore.MaxAge - TimeSpan.FromMinutes(1));

		var loaded = await CreateStore().LoadAsync(CreateSettings(GameSettings.Modus.Ziel));

		Assert.Null(loaded);
	}

	[Fact]
	public async Task AutoSave_ImTraining_SchreibtNichtUndLaesstDateiUnberuehrt()
	{
		WriteStateFile(GameSettings.Modus.Ziel);
		var before = File.ReadAllText(GameStateStore.FilePath);

		using var store = CreateStore();
		await store.StartAsync(CancellationToken.None);
		store.StartAutoSave(() => null);
		store.RequestSave();
		await Task.Delay(300);
		await store.StopAsync(CancellationToken.None);

		Assert.True(File.Exists(GameStateStore.FilePath));
		Assert.Equal(before, File.ReadAllText(GameStateStore.FilePath));
	}

	[Fact]
	public async Task AutoSave_ImSpielmodus_SchreibtDenStand()
	{
		using var store = CreateStore();
		await store.StartAsync(CancellationToken.None);
		store.StartAutoSave(() => new GameStateFile
		{
			BahnNummer = Bahn,
			Modus = GameSettings.Modus.Ziel,
			Ziel = new ZielState { Schiessen = [5] }
		});
		store.RequestSave();

		// Das Schreiben laeuft ueber eine Warteschlange - kurz auf die Datei warten.
		for (int i = 0; i < 20 && !File.Exists(GameStateStore.FilePath); i++)
			await Task.Delay(100);

		await store.StopAsync(CancellationToken.None);

		Assert.True(File.Exists(GameStateStore.FilePath));
		Assert.Contains("\"Schiessen\"", File.ReadAllText(GameStateStore.FilePath));
	}
}
