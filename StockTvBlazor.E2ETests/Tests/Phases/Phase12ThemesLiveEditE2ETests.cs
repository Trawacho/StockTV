using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Linq;

namespace StockTvBlazor.E2ETests.Tests.Phases;

/// <summary>
/// Deckt die Themes-Seiten-Überarbeitung ab: Live-Apply für bestehende Themes (Speichern nur
/// noch zum Anlegen neuer Themes nötig), Footer-Sichtbarkeit je nach Zustand, Verschieben der
/// Schriftart-Auswahl in den "Schrift"-Tab, und dass der Speichern-Button auch bei kleinem
/// Viewport per Scroll erreichbar bleibt.
/// </summary>
[Collection("E2E Sequential")]
public class Phase12ThemesLiveEditE2ETests : PhaseTestBase
{
	public Phase12ThemesLiveEditE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 12";
	}

	#region PRIVATE HELPERS

	private async Task NavigateToThemesTabAsync()
	{
		if (Fixture.Page == null)
			throw new InvalidOperationException("Page is null");

		Log(CurrentPhase, "Navigiere zu /themes");
		await Fixture.Page.GotoAsync("http://localhost:5001/themes");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(500);
	}

	private async Task NavigateToSchriftTabAsync()
	{
		await NavigateToThemesTabAsync();
		Log(CurrentPhase, "Klicke auf Tab 'Schrift'");
		await Fixture.Page!.Locator(".tab-btn", new() { HasTextString = "Schrift" }).ClickAsync();
		await Task.Delay(500);
	}

	private async Task CreateNewThemeDraftAsync(string name)
	{
		Log(CurrentPhase, $"Klicke '+ Neu', setze Namen = {name}");
		await Fixture.Page!.Locator(".btn-new").ClickAsync();
		await Task.Delay(300);
		var nameInput = Fixture.Page.Locator(".field input[type='text']").First;
		await nameInput.FillAsync(name);
		await Task.Delay(100);
	}

	private async Task ClickSaveButtonAsync()
	{
		Log(CurrentPhase, "Klicke 'Speichern'");
		await Fixture.Page!.Locator(".btn-save").ClickAsync();
		await Task.Delay(300);
	}

	/// <summary>
	/// Öffnet eine der Accordion-Sektionen im Theme-Editor ("Farben"/"Anspiel-Indikator
	/// Einstellungen"), falls sie aktuell eingeklappt ist (Default-Zustand).
	/// </summary>
	private async Task OpenEditorAccordionAsync(string label)
	{
		var button = Fixture.Page!.Locator(".accordion-button", new() { HasTextString = label });
		var isCollapsed = await button.EvaluateAsync<bool>("el => el.classList.contains('collapsed')");
		if (isCollapsed)
		{
			Log(CurrentPhase, $"Öffne Accordion-Sektion: {label}");
			await button.ClickAsync();
			await Task.Delay(300);
		}
	}

	private async Task FillColorFieldAsync(string fieldLabel, string hex)
	{
		Log(CurrentPhase, $"Setze Farbe '{fieldLabel}' = {hex}");
		var input = Fixture.Page!.Locator(
			$"xpath=//div[contains(@class,'color-field')][.//label[normalize-space()='{fieldLabel}']]//input[@type='text']"
		).First;
		await input.FillAsync(hex);
		await input.PressAsync("Tab");
		await Task.Delay(200);
	}

	/// <summary>
	/// Löscht ALLE Theme-Einträge mit dem gegebenen Namen (nicht nur den ersten) — Name ist nicht
	/// eindeutig, und ein Test-Cleanup soll auch bei mehreren gleichnamigen Karteileichen aus
	/// vorherigen fehlgeschlagenen Läufen robust aufräumen statt an einem Strict-Mode-Mehrfachtreffer
	/// zu scheitern.
	/// </summary>
	private async Task DeleteThemeByNameAsync(string name)
	{
		var themeItems = Fixture.Page!.Locator(".theme-item", new() { HasTextString = name });
		var count = await themeItems.CountAsync();
		Log(CurrentPhase, $"Lösche {count} Test-Theme(s) mit Namen '{name}'");
		for (var i = count - 1; i >= 0; i--)
		{
			await themeItems.Nth(i).Locator(".btn-delete").ClickAsync();
			await Task.Delay(200);
		}

		// DeleteCustomTheme speichert ebenfalls debounced (1000ms) — ohne diese Wartezeit kann
		// der App-Prozess am Ende des Testlaufs beendet werden, bevor die Löschung tatsächlich
		// auf Disk persistiert wurde, wodurch das Test-Theme für den nächsten Lauf liegen bleibt.
		if (count > 0)
			await Task.Delay(DEBOUNCE_DELAY_MS);
	}

	#endregion PRIVATE HELPERS

	/// <summary>
	/// Test 1: Eine Farbänderung an einem bestehenden (bereits gespeicherten) Theme wird ohne
	/// Klick auf "Speichern" nach Debounce persistiert.
	/// </summary>
	[Fact]
	public async Task Phase12_LiveApply_ColorChangeOnExistingThemePersistsWithoutSave()
	{
		LogPhaseStart(CurrentPhase, "Test 1: Live-Apply persistiert Farbänderung ohne Speichern-Klick");
		Assert.NotNull(Fixture.Page);

		const string themeName = "E2E LiveApply Theme";
		const string newHex = "#123456";

		try
		{
			await NavigateToThemesTabAsync();
			await CreateNewThemeDraftAsync(themeName);
			await ClickSaveButtonAsync(); // macht das Theme zu einem bestehenden Theme

			await OpenEditorAccordionAsync("Farben");
			await FillColorFieldAsync("Hintergrund", newHex);
			await Task.Delay(DEBOUNCE_DELAY_MS);

			Log(CurrentPhase, "Validiere Persistierung in stocktv.config.json...");
			// Name ist nicht eindeutig — es genügt, dass IRGENDEIN Eintrag mit diesem Namen
			// (nämlich der von diesem Testlauf erzeugte) die neue Farbe zeigt.
			var configJson = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
			{
				try
				{
					using var doc = JsonDocument.Parse(json);
					var customThemes = doc.RootElement.GetProperty("UI").GetProperty("CustomThemes");
					return customThemes.EnumerateArray().Any(theme =>
						theme.GetProperty("Name").GetString() == themeName &&
						theme.GetProperty("Colors").GetProperty("BackgroundColor").GetString() == newHex);
				}
				catch
				{
					return false;
				}
			});

			using var finalDoc = JsonDocument.Parse(configJson);
			var matchFound = finalDoc.RootElement.GetProperty("UI").GetProperty("CustomThemes").EnumerateArray()
				.Any(theme =>
					theme.GetProperty("Name").GetString() == themeName &&
					theme.GetProperty("Colors").GetProperty("BackgroundColor").GetString() == newHex);
			Assert.True(matchFound, $"Theme '{themeName}' mit Farbe {newHex} wurde nicht in der Config-Datei gefunden.");
			Log(CurrentPhase, "✓ Farbänderung wurde ohne Speichern-Klick persistiert", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
		finally
		{
			try
			{
				await DeleteThemeByNameAsync(themeName);
			}
			catch { }
		}
	}

	/// <summary>
	/// Test 2: Der Footer (Abbrechen/Speichern) ist nur beim Anlegen eines neuen Themes sichtbar,
	/// nicht mehr, sobald das Theme gespeichert (also "bestehend") ist.
	/// </summary>
	[Fact]
	public async Task Phase12_Footer_HiddenForExistingTheme_VisibleForNewTheme()
	{
		LogPhaseStart(CurrentPhase, "Test 2: Footer nur bei neuem Theme sichtbar");
		Assert.NotNull(Fixture.Page);

		const string themeName = "E2E Footer Theme";

		try
		{
			await NavigateToThemesTabAsync();
			await CreateNewThemeDraftAsync(themeName);

			var footerCountNew = await Fixture.Page!.Locator(".editor-footer").CountAsync();
			Assert.Equal(1, footerCountNew);
			Log(CurrentPhase, "✓ Footer sichtbar beim Anlegen eines neuen Themes", LogSymbol.Check);

			await ClickSaveButtonAsync();

			var footerCountExisting = await Fixture.Page.Locator(".editor-footer").CountAsync();
			Assert.Equal(0, footerCountExisting);
			Log(CurrentPhase, "✓ Footer ausgeblendet nach dem Speichern (Theme ist jetzt bestehend)", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
		finally
		{
			try
			{
				await DeleteThemeByNameAsync(themeName);
			}
			catch { }
		}
	}

	/// <summary>
	/// Test 3: Die Schriftart-Auswahl existiert nur noch im "Schrift"-Tab, nicht mehr im
	/// "Themes"-Tab.
	/// </summary>
	[Fact]
	public async Task Phase12_FontFamilyDropdown_OnlyInSchriftTab()
	{
		LogPhaseStart(CurrentPhase, "Test 3: Schriftart-Dropdown nur im Schrift-Tab");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToThemesTabAsync();
			var countInThemesTab = await Fixture.Page!.Locator(
				"xpath=//label[normalize-space()='Schriftart (optional)']"
			).CountAsync();
			Assert.Equal(0, countInThemesTab);
			Log(CurrentPhase, "✓ Schriftart-Auswahl nicht mehr im Themes-Tab", LogSymbol.Check);

			await NavigateToSchriftTabAsync();
			await Fixture.Page.Locator(".accordion-button", new() { HasTextString = "Schriftart" }).ClickAsync();
			await Task.Delay(300);
			var countInSchriftTab = await Fixture.Page.Locator(
				"xpath=//label[normalize-space()='Schriftart (optional)']"
			).CountAsync();
			Assert.Equal(1, countInSchriftTab);
			Log(CurrentPhase, "✓ Schriftart-Auswahl im Schrift-Tab vorhanden", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
	}

	/// <summary>
	/// Test 4: Regressionstest für den Scroll-Bug — bei kleinem Viewport muss der
	/// Speichern-Button beim Anlegen eines neuen Themes per Scroll erreichbar/klickbar bleiben.
	/// Playwrights Click-Aktion scrollt automatisch in den sichtbaren Bereich; schlägt der Scroll
	/// fehl (z.B. durch overflow:hidden ohne begrenzte Höhe), läuft der Klick in ein Timeout.
	/// </summary>
	[Fact]
	public async Task Phase12_SmallViewport_SaveButtonReachableViaScroll()
	{
		LogPhaseStart(CurrentPhase, "Test 4: Speichern-Button bei kleinem Viewport erreichbar");
		Assert.NotNull(Fixture.Page);

		const string themeName = "E2E Scroll Theme";

		try
		{
			await Fixture.Page!.SetViewportSizeAsync(800, 450);
			await NavigateToThemesTabAsync();
			await CreateNewThemeDraftAsync(themeName);

			// Klick scrollt automatisch in den sichtbaren Bereich — schlägt fehl (Timeout),
			// falls die Seite nicht scrollen kann.
			await ClickSaveButtonAsync();

			var footerCountExisting = await Fixture.Page.Locator(".editor-footer").CountAsync();
			Assert.Equal(0, footerCountExisting);
			Log(CurrentPhase, "✓ Speichern-Button war trotz kleinem Viewport klickbar (Scroll funktioniert)", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
		finally
		{
			try
			{
				await DeleteThemeByNameAsync(themeName);
			}
			catch { }
			try
			{
				await Fixture.Page!.SetViewportSizeAsync(1280, 720);
			}
			catch { }
		}
	}
}
