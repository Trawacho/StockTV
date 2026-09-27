using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

/// <summary>
/// Phase 13: Modus-Schnellwechsel auf /input (Press-and-Hold Modus-Bearbeitung).
/// Deckt den neuen Bearbeitungsmodus ab, der über das ".display"-Feld auf der Input-Seite
/// aktiviert wird: 5 Sekunden gedrückt halten schaltet das Numpad auf up/prev/next/down um,
/// "4"/"6" blättern durch die Modi, "+" bestätigt (speichert + navigiert). Die Funktion ist nur
/// nutzbar, solange kein Subscriber (StockApp) am NetMQ-PUB-Socket verbunden ist
/// (General.BlockLocalChanges).
///
/// WICHTIG: Diese Tests laufen SEQUENZIELL mit allen anderen E2E Tests!
/// </summary>
[Collection("E2E Sequential")]
public class Phase13ModusQuickSwitchE2ETests : PhaseTestBase
{
	private const string InputUrl = "http://localhost:5001/input";

	public Phase13ModusQuickSwitchE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 13";
	}

	[Fact]
	public async Task Phase13_ModusQuickSwitch_DisabledWhileSubscriberConnected_ThenFullFlow()
	{
		LogPhaseStart("Phase 13", "Modus-Schnellwechsel auf /input (Press-and-Hold)");
		Assert.NotNull(Fixture.Page);

		try
		{
			Log(CurrentPhase, "Stelle definierten Ausgangszustand her (Training)...");
			await ConfigureAndValidateSettings(modus: 0, maxPunkteProKehre: 15, maxKehrenProSpiel: 30);

			await Fixture.Page!.GotoAsync(InputUrl);
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1000);

			// --- Teil A: Deaktiviert, solange ein Subscriber (StockApp) verbunden ist ---
			// Der Test-Fixture-Subscriber ist seit AppFixture.InitializeNetMQ() dauerhaft
			// verbunden -> General.BlockLocalChanges ist bereits true, bevor dieser Test
			// überhaupt etwas tut. Das ist also der reguläre Ausgangszustand aller E2E-Tests.
			Log(CurrentPhase, "Prüfe: Feld ist deaktiviert, solange ein Subscriber verbunden ist...");

			var displayClassesConnected = await Fixture.Page.Locator(".display").GetAttributeAsync("class");
			Assert.DoesNotContain("clickable", displayClassesConnected ?? "");
			Log(CurrentPhase, "'.display' hat KEINE 'clickable'-Klasse bei verbundenem Subscriber", LogSymbol.Check);

			await PressAndHoldDisplayAsync(5500);
			await Task.Delay(300);

			var panelClassesConnected = await Fixture.Page.Locator(".keypad-panel").GetAttributeAsync("class");
			Assert.DoesNotContain("modus-edit-active", panelClassesConnected ?? "");
			Log(CurrentPhase, "5s-Halten bei verbundenem Subscriber aktiviert die Bearbeitung NICHT", LogSymbol.Check);

			// --- Teil B: Subscriber trennen -> BlockLocalChanges wird false ---
			Log(CurrentPhase, "Trenne Test-Subscriber (simuliert StockApp-Verbindungsabbau)...");
			Fixture.SetPublisherSubscriptionActive(false);

			var becameClickable = await GameplayScriptHelpers.PollUntilAsync(
				async () =>
				{
					var cls = await Fixture.Page.Locator(".display").GetAttributeAsync("class");
					return cls?.Contains("clickable") == true;
				},
				TimeSpan.FromSeconds(5));
			Assert.True(becameClickable, "'.display' sollte nach Trennen des Subscribers 'clickable' werden");
			Log(CurrentPhase, "Feld wird nach Trennen des Subscribers klickbar", LogSymbol.Check);

			// --- Teil C: Kurzer Tap (< 5s) aktiviert NICHTS ---
			await PressAndHoldDisplayAsync(1000);
			await Task.Delay(200);

			var panelClassesShortTap = await Fixture.Page.Locator(".keypad-panel").GetAttributeAsync("class");
			Assert.DoesNotContain("modus-edit-active", panelClassesShortTap ?? "");
			Log(CurrentPhase, "Kurzer Tap (1s) aktiviert die Bearbeitung nicht", LogSymbol.Check);

			// --- Teil D: 5s+ Halten aktiviert die Bearbeitung ---
			await PressAndHoldDisplayAsync(5500);
			await Task.Delay(300);

			var panelClassesActive = await Fixture.Page.Locator(".keypad-panel").GetAttributeAsync("class");
			Assert.Contains("modus-edit-active", panelClassesActive ?? "");

			var gridTexts = await Fixture.Page.Locator(".numbers-grid .key").AllTextContentsAsync();
			Assert.Contains("prev", gridTexts);
			Assert.Contains("next", gridTexts);
			Assert.Contains("up", gridTexts);
			Assert.Contains("down", gridTexts);
			Log(CurrentPhase, "5s-Halten aktiviert Bearbeitung, Numpad zeigt up/prev/next/down", LogSymbol.Check);

			// --- Teil E: Ziffern-/Farbtasten sind während der Bearbeitung wirkungslos ---
			var displayTextBeforeNoop = (await Fixture.Page.Locator(".display").TextContentAsync())?.Trim();
			await Fixture.Page.Locator(".numbers-grid .key").First.ClickAsync(); // Position "7", leer beschriftet
			await Task.Delay(200);

			var displayTextAfterNoop = (await Fixture.Page.Locator(".display").TextContentAsync())?.Trim();
			Assert.Equal(displayTextBeforeNoop, displayTextAfterNoop);

			var panelClassesAfterNoop = await Fixture.Page.Locator(".keypad-panel").GetAttributeAsync("class");
			Assert.Contains("modus-edit-active", panelClassesAfterNoop ?? "");
			Log(CurrentPhase, "Ziffern-Taste während der Bearbeitung wirkungslos (Modus unverändert)", LogSymbol.Check);

			// --- Teil F: "next" (6) wechselt den Modus live, Iframe bleibt eingefroren ---
			var iframeSrcBeforeConfirm = await Fixture.Page.Locator("iframe").GetAttributeAsync("src");

			await Fixture.Page.Locator(".numbers-grid .key:text-is('next')").ClickAsync();
			await Task.Delay(300);

			var displayTextAfterNext = (await Fixture.Page.Locator(".display").TextContentAsync())?.Trim();
			Assert.Equal("BestOf", displayTextAfterNext);

			var iframeSrcAfterNext = await Fixture.Page.Locator("iframe").GetAttributeAsync("src");
			Assert.Equal(iframeSrcBeforeConfirm, iframeSrcAfterNext);
			Log(CurrentPhase, $"'next' wechselt Anzeige zu '{displayTextAfterNext}', Iframe bleibt eingefroren ({iframeSrcAfterNext})", LogSymbol.Check);

			// --- Teil G: "+" bestätigt: speichert, navigiert, beendet die Bearbeitung ---
			await Fixture.Page.Locator("button:text-is('Bestätigen')").ClickAsync();
			await Task.Delay(300);

			var panelClassesAfterConfirm = await Fixture.Page.Locator(".keypad-panel").GetAttributeAsync("class");
			Assert.DoesNotContain("modus-edit-active", panelClassesAfterConfirm ?? "");

			var iframeSrcAfterConfirm = await Fixture.Page.Locator("iframe").GetAttributeAsync("src");
			Assert.Equal("/bestof", iframeSrcAfterConfirm);

			var gridTextsAfterConfirm = await Fixture.Page.Locator(".numbers-grid .key").AllTextContentsAsync();
			Assert.Contains("7", gridTextsAfterConfirm);
			Log(CurrentPhase, "'+' bestätigt: Iframe navigiert zu /bestof, Numpad zeigt wieder Ziffern", LogSymbol.Check);

			await ValidateSettingsPersistenceAsync(expectedModus: 1, expectedMaxPunkteProKehre: 10, expectedMaxKehrenProSpiel: 6);
			Log(CurrentPhase, "Modus-Wechsel korrekt in stocktv.config.json persistiert", LogSymbol.Check);
		}
		finally
		{
			// Subscriber wiederherstellen: BlockLocalChanges=true entspricht dem Ausgangszustand,
			// den alle anderen Phasen erwarten (dauerhaft verbundener Test-Subscriber).
			Fixture.SetPublisherSubscriptionActive(true);
			LogPhaseEnd("Phase 13");
		}
	}

	private async Task PressAndHoldDisplayAsync(int holdMs)
	{
		Assert.NotNull(Fixture.Page);
		var display = Fixture.Page!.Locator(".display");
		var box = await display.BoundingBoxAsync();
		Assert.NotNull(box);

		var cx = box!.X + box.Width / 2;
		var cy = box.Y + box.Height / 2;

		await Fixture.Page.Mouse.MoveAsync(cx, cy);
		await Fixture.Page.Mouse.DownAsync();
		await Task.Delay(holdMs);
		await Fixture.Page.Mouse.UpAsync();
	}
}
