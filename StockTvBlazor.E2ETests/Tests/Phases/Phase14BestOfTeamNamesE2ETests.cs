using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests.Phases;

/// <summary>
/// Phase 14: Teamnamen-Eingabe auf /input (BestOf-Modus).
/// Deckt den neuen Team-Button (Namensschild-Icon) im Numpad-Grid ab (unterhalb "2", rechts von "0"): öffnet zwei
/// Eingabefelder + Speichern-Button unterhalb der virtuellen Tastatur, mit dem die beiden
/// Teamnamen für die Spiele 1-7 direkt am Gerät gesetzt werden können. Nur nutzbar, solange
/// Modus=BestOf, keine Punkte im aktuellen Match erfasst sind und kein Subscriber (StockApp) am
/// NetMQ-PUB-Socket verbunden ist (General.BlockLocalChanges).
///
/// WICHTIG: Diese Tests laufen SEQUENZIELL mit allen anderen E2E Tests!
/// </summary>
[Collection("E2E Sequential")]
public class Phase14BestOfTeamNamesE2ETests : PhaseTestBase
{
	private const string InputUrl = "http://localhost:5001/input";
	private const string BestOfUrl = "http://localhost:5001/bestof";

	public Phase14BestOfTeamNamesE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 14";
	}

	[Fact]
	public async Task Phase14_BestOfTeamNames_FullFlow()
	{
		LogPhaseStart("Phase 14", "Teamnamen-Eingabe auf /input (BestOf)");
		Assert.NotNull(Fixture.Page);

		try
		{
			Log(CurrentPhase, "Stelle definierten Ausgangszustand her (BestOf, Richtung=Rechts, frisches Match)...");
			await ConfigureAndValidateSettings(modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6, richtung: 1);
			await SendResetResult();

			// Wie bei Phase 13: der Test-Fixture-Subscriber ist standardmäßig verbunden
			// (BlockLocalChanges=true) - für diesen Test muss er getrennt werden, da
			// CanEditTeamNames denselben Guard nutzt.
			Log(CurrentPhase, "Trenne Test-Subscriber (simuliert StockApp-Verbindungsabbau)...");
			Fixture.SetPublisherSubscriptionActive(false);

			await Fixture.Page!.GotoAsync(InputUrl);
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1000);

			// --- Teil A: Button sichtbar bei BestOf + 0 Punkte + kein Subscriber ---
			var iconBtn = Fixture.Page.Locator(".numbers-grid .key:has(svg.team-icon)");
			await Task.Delay(500); // BlockLocalChanges-Propagierung abwarten (asynchron über NetMQ-Poller)
			Assert.Equal(1, await iconBtn.CountAsync());
			Log(CurrentPhase, "Team-Button (Namensschild-Icon) sichtbar (BestOf, 0 Punkte, kein Subscriber)", LogSymbol.Check);

			// --- Teil B: Klick öffnet den Editor mit leeren Feldern ---
			await iconBtn.ClickAsync();
			await Task.Delay(200);

			Assert.Equal(1, await Fixture.Page.Locator(".team-name-editor").CountAsync());
			Assert.Equal("", await Fixture.Page.Locator(".team-name-input-left").InputValueAsync());
			Assert.Equal("", await Fixture.Page.Locator(".team-name-input-right").InputValueAsync());
			Log(CurrentPhase, "Editor öffnet mit zwei leeren Feldern", LogSymbol.Check);

			// --- Teil C: Rahmenfarben entsprechen G/R (Richtung=Rechts -> links=Grün, rechts=Rot) ---
			var leftBorder = await Fixture.Page.Locator(".team-name-input-left").EvaluateAsync<string>("el => getComputedStyle(el).borderColor");
			var rightBorder = await Fixture.Page.Locator(".team-name-input-right").EvaluateAsync<string>("el => getComputedStyle(el).borderColor");
			Assert.Equal("rgb(0, 128, 0)", leftBorder);
			Assert.Equal("rgb(255, 0, 0)", rightBorder);
			Log(CurrentPhase, "Rahmenfarben entsprechen G/R-Tasten (links=Grün, rechts=Rot)", LogSymbol.Check);

			// --- Teil D: Namen eintippen, "✕" testen, erneut befüllen, speichern ---
			await Fixture.Page.Locator(".team-name-input-left").FillAsync("Team Alpha");
			await Fixture.Page.Locator(".team-name-input-right").FillAsync("Team Beta");

			await Fixture.Page.Locator(".team-name-clear").First.ClickAsync();
			await Task.Delay(150);
			Assert.Equal("", await Fixture.Page.Locator(".team-name-input-left").InputValueAsync());
			Assert.Equal("Team Beta", await Fixture.Page.Locator(".team-name-input-right").InputValueAsync());
			Log(CurrentPhase, "'✕' leert nur das jeweilige Feld", LogSymbol.Check);

			await Fixture.Page.Locator(".team-name-input-left").FillAsync("Team Alpha");
			await Fixture.Page.Locator("button:text-is('Speichern')").ClickAsync();
			await Task.Delay(300);

			Assert.Equal(0, await Fixture.Page.Locator(".team-name-editor").CountAsync());
			Log(CurrentPhase, "'Speichern' schließt den Editor", LogSymbol.Check);

			// Button bleibt sichtbar - weiterhin 0 Punkte im Match.
			Assert.Equal(1, await iconBtn.CountAsync());

			// --- Teil E: /bestof zeigt die Namen an der richtigen Seite (Richtung=Rechts) ---
			await Fixture.Page.GotoAsync(BestOfUrl);
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(500);

			Assert.Equal("Team Alpha", (await Fixture.Page.Locator(".teamname-left").TextContentAsync())?.Trim());
			Assert.Equal("Team Beta", (await Fixture.Page.Locator(".teamname-right").TextContentAsync())?.Trim());
			Log(CurrentPhase, "/bestof zeigt Namen korrekt links/rechts (Richtung=Rechts)", LogSymbol.Check);

			// --- Teil F: Richtung wechseln -> Namen spiegeln sich auf /bestof ---
			await ConfigureAndValidateSettings(modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6, richtung: 0);
			await Fixture.Page.ReloadAsync();
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(500);

			Assert.Equal("Team Beta", (await Fixture.Page.Locator(".teamname-left").TextContentAsync())?.Trim());
			Assert.Equal("Team Alpha", (await Fixture.Page.Locator(".teamname-right").TextContentAsync())?.Trim());
			Log(CurrentPhase, "Richtungswechsel spiegelt die Teamnamen auf /bestof korrekt", LogSymbol.Check);

			// Richtung für den Rest des Tests zurücksetzen (Rechts, Ausgangszustand).
			await ConfigureAndValidateSettings(modus: 1, maxPunkteProKehre: 10, maxKehrenProSpiel: 6, richtung: 1);

			// --- Teil G: Erneutes Öffnen zeigt vorbefüllte Felder ---
			await Fixture.Page.GotoAsync(InputUrl);
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1000);

			await iconBtn.ClickAsync();
			await Task.Delay(200);
			Assert.Equal("Team Alpha", await Fixture.Page.Locator(".team-name-input-left").InputValueAsync());
			Assert.Equal("Team Beta", await Fixture.Page.Locator(".team-name-input-right").InputValueAsync());
			Log(CurrentPhase, "Erneutes Öffnen zeigt vorbefüllte, zuvor gespeicherte Namen", LogSymbol.Check);

			// Schließen ohne zu speichern (Toggle) - muss gefahrlos möglich sein.
			await iconBtn.ClickAsync();
			await Task.Delay(200);
			Assert.Equal(0, await Fixture.Page.Locator(".team-name-editor").CountAsync());

			// --- Teil H: Nach einer erfassten Kehre ist der Button gesperrt ---
			await Fixture.Page.Locator(".numbers-grid .key:text-is('3')").ClickAsync();
			await Task.Delay(200);
			await Fixture.Page.Locator(".color-row-double .key").First.ClickAsync();
			await Task.Delay(400);

			Assert.Equal(0, await iconBtn.CountAsync());
			Log(CurrentPhase, "Nach erfasster Kehre ist der Button gesperrt (leer)", LogSymbol.Check);

			await SendResetResult();

			// --- Teil I: In Training ist der Button ebenfalls gesperrt ---
			await ConfigureAndValidateSettings(modus: 0, maxPunkteProKehre: 15, maxKehrenProSpiel: 30);
			await Fixture.Page.GotoAsync(InputUrl);
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(1000);

			Assert.Equal(0, await iconBtn.CountAsync());
			Log(CurrentPhase, "Im Training-Modus bleibt der Button funktionslos", LogSymbol.Check);
		}
		finally
		{
			// Subscriber wiederherstellen: BlockLocalChanges=true entspricht dem Ausgangszustand,
			// den alle anderen Phasen erwarten (dauerhaft verbundener Test-Subscriber).
			Fixture.SetPublisherSubscriptionActive(true);
			LogPhaseEnd("Phase 14");
		}
	}
}
