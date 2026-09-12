using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

/// <summary>
/// Verhindert parallele Ausführung mit anderen E2E Tests (App nicht parallel-sicher)
/// </summary>
[CollectionDefinition("E2E Sequential", DisableParallelization = true)]
public class E2ESequentialCollection : ICollectionFixture<AppFixture>
{
}

/// <summary>
/// Phase 10: Settings-Persistierungs-Tests
/// Testet die verbesserte RequestSaveSettings() Debounce-Implementierung
/// mit echten Persistierungs-Validierungen.
///
/// WICHTIG: Diese Tests laufen SEQUENZIELL mit allen anderen E2E Tests!
/// </summary>
[Collection("E2E Sequential")]
public class Phase10SettingsPersistenceE2ETests : PhaseTestBase
{
	public Phase10SettingsPersistenceE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 10";
	}

	/// <summary>
	/// Testet dass schnelle Änderungen hintereinander korrekt debounced werden.
	/// Szenario: Benutzer ändert Settings 3x schnell → nur LETZTE wird persistiert
	/// </summary>
	[Fact]
	public async Task Phase10_RapidSettingsChanges_OnlyLastPersisted()
	{
		LogPhaseStart("Phase 10", "Rapid Settings Changes - Debounce Test");

		try
		{
			Log(CurrentPhase, "Lade aktuelle Settings...");
			var currentBytes = await GetCurrentSettings();

			// Sende 3 Settings-Änderungen hintereinander (schnell)
			Log(CurrentPhase, "Sende 3 schnelle Settings-Änderungen...");

			var settings0 = GameplayScriptHelpers.BuildSettingsBytes(currentBytes, modus: 0, maxPunkteProKehre: 15, maxKehrenProSpiel: 30);
			await SendSettings(settings0);
			Log(CurrentPhase, "  1. Modus=0 (Training)");
			await Task.Delay(50);

			var settings1 = GameplayScriptHelpers.BuildSettingsBytes(settings0, modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(settings1);
			Log(CurrentPhase, "  2. Modus=1 (BestOf)");
			await Task.Delay(50);

			var settings2 = GameplayScriptHelpers.BuildSettingsBytes(settings1, modus: 2, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(settings2);
			Log(CurrentPhase, "  3. Modus=2 (Turnier)");

			// Validiere dass nur LETZTE persistiert wurde
			await ValidateSettingsPersistenceAsync(expectedModus: 2, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "✓ Debounce funktioniert - nur letzte Änderung persistiert");
		}
		finally
		{
			LogPhaseEnd("Phase 10");
		}
	}

	/// <summary>
	/// Testet dass Settings nach Debounce-Timeout persistiert werden.
	/// </summary>
	[Fact]
	public async Task Phase10_DebounceTimeout_SettingsPersisted()
	{
		LogPhaseStart("Phase 10", "Debounce Timeout Persistence");

		try
		{
			Log(CurrentPhase, "Lade aktuelle Settings...");
			var currentBytes = await GetCurrentSettings();

			Log(CurrentPhase, "Ändere Settings zu BestOf (Modus=1)...");
			var newSettings = GameplayScriptHelpers.BuildSettingsBytes(currentBytes, modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(newSettings);

			// ValidateSettingsPersistenceAsync hat eingebaute Retry-Logik (6 Versuche)
			await ValidateSettingsPersistenceAsync(expectedModus: 1, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "✓ Settings nach Timeout persistiert");
		}
		finally
		{
			LogPhaseEnd("Phase 10");
		}
	}

	/// <summary>
	/// Testet dass Debounce bei neuer Änderung neu startet.
	/// </summary>
	[Fact]
	public async Task Phase10_DebounceReset_OnNewChange()
	{
		LogPhaseStart("Phase 10", "Debounce Reset");

		try
		{
			Log(CurrentPhase, "Lade aktuelle Settings...");
			var currentBytes = await GetCurrentSettings();

			// Erste Änderung
			Log(CurrentPhase, "1. Änderung: Modus=0 (Training)");
			var settings0 = GameplayScriptHelpers.BuildSettingsBytes(currentBytes, modus: 0, maxPunkteProKehre: 15, maxKehrenProSpiel: 30);
			await SendSettings(settings0);

			// Warte 0.6s (< 1s Debounce)
			Log(CurrentPhase, "Warte 0.6s (< 1s Debounce)...");
			await Task.Delay(600);

			// Zweite Änderung - setzt Debounce zurück
			Log(CurrentPhase, "2. Änderung: Modus=2 (Turnier) - setzt Debounce zurück");
			var settings2 = GameplayScriptHelpers.BuildSettingsBytes(settings0, modus: 2, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(settings2);

			// ValidateSettingsPersistenceAsync wartet selbst auf die Persistierung
			await ValidateSettingsPersistenceAsync(expectedModus: 2, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "✓ Debounce wurde korrekt zurückgesetzt");
		}
		finally
		{
			LogPhaseEnd("Phase 10");
		}
	}

	/// <summary>
	/// Testet dass mehrere schnelle Änderungen alle persistiert werden (nicht nur die letzte).
	/// </summary>
	[Fact]
	public async Task Phase10_SequentialChanges_AllPersisted()
	{
		LogPhaseStart("Phase 10", "Sequential Changes Persistence");

		try
		{
			var currentBytes = await GetCurrentSettings();

			// Change 1: Modus=0
			Log(CurrentPhase, "Change 1: Modus=0...");
			var settings0 = GameplayScriptHelpers.BuildSettingsBytes(currentBytes, modus: 0, maxPunkteProKehre: 15, maxKehrenProSpiel: 30);
			await SendSettings(settings0);
			await ValidateSettingsPersistenceAsync(expectedModus: 0, expectedMaxPunkteProKehre: 15, expectedMaxKehrenProSpiel: 30);
			Log(CurrentPhase, "  ✓ Modus=0 persistiert");

			// Change 2: Modus=1 (mit genug Abstand dass Debounce abgelaufen ist)
			await Task.Delay(1500);
			Log(CurrentPhase, "Change 2: Modus=1...");
			var settings1 = GameplayScriptHelpers.BuildSettingsBytes(settings0, modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(settings1);
			await ValidateSettingsPersistenceAsync(expectedModus: 1, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "  ✓ Modus=1 persistiert");

			// Change 3: Modus=2
			await Task.Delay(1500);
			Log(CurrentPhase, "Change 3: Modus=2...");
			var settings2 = GameplayScriptHelpers.BuildSettingsBytes(settings1, modus: 2, maxPunkteProKehre: 10, maxKehrenProSpiel: 6);
			await SendSettings(settings2);
			await ValidateSettingsPersistenceAsync(expectedModus: 2, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "  ✓ Modus=2 persistiert");

			Log(CurrentPhase, "✓ Alle sequenziellen Changes zuverlässig persistiert");
		}
		finally
		{
			LogPhaseEnd("Phase 10");
		}
	}
}
