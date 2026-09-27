using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase11FontWeightE2ETests : PhaseTestBase
{
	public Phase11FontWeightE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 11";
	}

	#region PRIVATE HELPERS

	/// <summary>
	/// Navigates to /themes and clicks the "Schriftstärke" tab.
	/// </summary>
	private async Task NavigateToFontWeightTabAsync()
	{
		if (Fixture.Page == null)
			throw new InvalidOperationException("Page is null");

		Log(CurrentPhase, "Navigiere zu /themes");
		await Fixture.Page.GotoAsync("http://localhost:5001/themes");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(500);

		Log(CurrentPhase, "Klicke auf Tab 'Schriftstärke'");
		var tabButton = Fixture.Page.Locator(".tab-btn", new() { HasTextString = "Schriftstärke" });
		await tabButton.ClickAsync();
		await Task.Delay(500);
	}

	/// <summary>
	/// Encodes a literal value for use inside an XPath expression, choosing single or double
	/// quotes based on the value's content (XPath 1.0 has no escape character for quotes).
	/// Needed because some labels (e.g. "Trenner ':'") contain apostrophes themselves.
	/// </summary>
	private static string XPathLiteral(string value)
	{
		if (!value.Contains('\''))
			return $"'{value}'";
		if (!value.Contains('"'))
			return $"\"{value}\"";
		var parts = value.Split('\'');
		return "concat('" + string.Join("', \"'\", '", parts) + "')";
	}

	/// <summary>
	/// Builds an XPath matching the &lt;select&gt; inside the .font-weight-field div whose
	/// label's own text node (excluding the info-icon span) equals fieldLabel exactly.
	/// Exact-match on the direct text node avoids any risk of substring collisions between
	/// field labels (e.g. "Gesamt" vs "Gesamtpunkte") — the root cause class of bug found and
	/// fixed in Phase 9.
	/// </summary>
	private static string FieldXPath(string fieldLabel) =>
		$"xpath=//div[contains(@class,'font-weight-field')][.//label/text()[normalize-space()={XPathLiteral(fieldLabel)}]]//select";

	private async Task SelectFontWeightAsync(string fieldLabel, string value)
	{
		if (Fixture.Page == null)
			return;

		Log(CurrentPhase, $"Setze {fieldLabel} = {value}");
		var select = Fixture.Page.Locator(FieldXPath(fieldLabel)).First;
		await select.SelectOptionAsync(value);
		await Task.Delay(100);
	}

	private async Task<string> GetFontWeightValueAsync(string fieldLabel)
	{
		if (Fixture.Page == null)
			return "";

		var select = Fixture.Page.Locator(FieldXPath(fieldLabel)).First;
		return await select.InputValueAsync() ?? "";
	}

	private async Task AssertFontWeightValueAsync(string fieldLabel, string expectedValue)
	{
		var actual = await GetFontWeightValueAsync(fieldLabel);
		Assert.True(expectedValue == actual, $"Field '{fieldLabel}' should be {expectedValue}, but got {actual}");
		Log(CurrentPhase, $"✓ Feld {fieldLabel} = {actual} (erwartet: {expectedValue})", LogSymbol.Check);
	}

	/// <summary>
	/// Clicks the "Alle Werte zurücksetzen" button (shared markup/class with TableLayoutEditor,
	/// but only one is ever mounted at a time since the tabs are mutually exclusive).
	/// </summary>
	private async Task ClickResetAllButtonAsync()
	{
		if (Fixture.Page == null)
			return;

		Log(CurrentPhase, "Klicke auf 'Alle Werte zurücksetzen' Button");
		var resetBtn = Fixture.Page.Locator(".btn-cancel", new() { HasTextString = "Alle Werte zurücksetzen" });
		await resetBtn.ClickAsync();
		await Task.Delay(500);
	}

	private static readonly (string Label, string Default)[] AllFields =
	[
		("Team-Namen (Turnier / BestOf)", "400"),
		("Kopfzeile (Training / Turnier / BestOf)", "700"),
		("Punkte-Summe (Spielstand)", "700"),
		("Aktuelle Kehre-Punkte", "700"),
		("Eingabe (Tippbuffer)", "700"),
		("Trenner ':'", "600"),
		("Matchpunkte (BestOf)", "200"),
		("Spielername", "600"),
		("Versuche", "500"),
		("Gesamt", "500"),
		("Letzter Wert", "500"),
		("Ziel-Eingabe", "500"),
		("Gesamtpunkte", "500"),
		("Summe", "500"),
	];

	#endregion PRIVATE HELPERS

	/// <summary>
	/// Test 1: Default values display correctly for all 14 font-weight fields.
	/// </summary>
	[Fact]
	public async Task Phase11_DefaultValuesDisplayCorrectly()
	{
		LogPhaseStart(CurrentPhase, "Test 1: Standard-Werte korrekt angezeigt");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			foreach (var (label, defaultValue) in AllFields)
			{
				await AssertFontWeightValueAsync(label, defaultValue);
			}

			Log(CurrentPhase, "Alle Standard-Werte korrekt angezeigt", LogSymbol.Check);
			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
	}

	/// <summary>
	/// Test 2: Value change persists to config file after debounce.
	/// </summary>
	[Fact]
	public async Task Phase11_ValueChange_PersistsToConfigFile()
	{
		LogPhaseStart(CurrentPhase, "Test 2: Wert-Änderung wird persistiert");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await SelectFontWeightAsync("Summe", "900");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			Log(CurrentPhase, "Validiere Persistierung in stocktv.config.json...");
			var configJson = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
			{
				try
				{
					using var doc = JsonDocument.Parse(json);
					var weight = doc.RootElement.GetProperty("UI").GetProperty("CellFontWeight").GetProperty("ZielSummeWeight").GetDouble();
					return Math.Abs(weight - 900) < 0.01;
				}
				catch
				{
					return false;
				}
			});

			using var finalDoc = JsonDocument.Parse(configJson);
			var zielSummeWeight = finalDoc.RootElement.GetProperty("UI").GetProperty("CellFontWeight").GetProperty("ZielSummeWeight").GetDouble();

			Assert.True(Math.Abs(zielSummeWeight - 900) < 0.01);
			Log(CurrentPhase, $"✓ ZielSummeWeight = {zielSummeWeight} in Datei persistiert", LogSymbol.Check);

			// Cleanup
			await SelectFontWeightAsync("Summe", "500");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			try
			{
				await SelectFontWeightAsync("Summe", "500");
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
			catch { }
			throw;
		}
	}

	/// <summary>
	/// Test 3: Reset all button restores defaults in UI and file.
	/// </summary>
	[Fact]
	public async Task Phase11_ResetAllValues_RestoresDefaultsInUiAndFile()
	{
		LogPhaseStart(CurrentPhase, "Test 3: 'Alle Werte zurücksetzen' stellt Defaults wieder her");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();

			await SelectFontWeightAsync("Team-Namen (Turnier / BestOf)", "900");
			await SelectFontWeightAsync("Summe", "100");

			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await AssertFontWeightValueAsync("Team-Namen (Turnier / BestOf)", "400");
			await AssertFontWeightValueAsync("Summe", "500");

			Log(CurrentPhase, "Validiere config.json nach Reset...");
			var configJson = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
			{
				try
				{
					using var doc = JsonDocument.Parse(json);
					var cellFontWeight = doc.RootElement.GetProperty("UI").GetProperty("CellFontWeight");
					var teamName = cellFontWeight.GetProperty("TeamNameWeight").GetDouble();
					var summe = cellFontWeight.GetProperty("ZielSummeWeight").GetDouble();
					return Math.Abs(teamName - 400) < 0.01 && Math.Abs(summe - 500) < 0.01;
				}
				catch
				{
					return false;
				}
			});

			using var finalDoc = JsonDocument.Parse(configJson);
			var teamNameWeight = finalDoc.RootElement.GetProperty("UI").GetProperty("CellFontWeight").GetProperty("TeamNameWeight").GetDouble();
			Assert.True(Math.Abs(teamNameWeight - 400) < 0.01);
			Log(CurrentPhase, "✓ Config-Datei zeigt Default-Werte nach Reset", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			try
			{
				await ClickResetAllButtonAsync();
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
			catch { }
			throw;
		}
	}

	/// <summary>
	/// Test 4: The 6 Ziel value-cell fields are independently editable — changing one must not
	/// affect the others (regression guard for the former shared .ziel-cell rule).
	/// </summary>
	[Fact]
	public async Task Phase11_ZielFields_IndependentlyEditable()
	{
		LogPhaseStart(CurrentPhase, "Test 4: Ziel-Felder unabhängig editierbar");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await SelectFontWeightAsync("Summe", "900");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await AssertFontWeightValueAsync("Summe", "900");
			await AssertFontWeightValueAsync("Versuche", "500");
			await AssertFontWeightValueAsync("Gesamt", "500");
			await AssertFontWeightValueAsync("Letzter Wert", "500");
			await AssertFontWeightValueAsync("Ziel-Eingabe", "500");
			await AssertFontWeightValueAsync("Gesamtpunkte", "500");
			await AssertFontWeightValueAsync("Spielername", "600");

			// Cleanup
			await SelectFontWeightAsync("Summe", "500");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			try
			{
				await SelectFontWeightAsync("Summe", "500");
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
			catch { }
			throw;
		}
	}

	/// <summary>
	/// Test 5: The 3 Training/Turnier/BestOf point-cell fields are independently editable —
	/// regression guard for the former shared .score-cell rule.
	/// </summary>
	[Fact]
	public async Task Phase11_ScoreCellFields_IndependentlyEditable()
	{
		LogPhaseStart(CurrentPhase, "Test 5: Punkte-Zellen-Felder unabhängig editierbar");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await SelectFontWeightAsync("Eingabe (Tippbuffer)", "100");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await AssertFontWeightValueAsync("Eingabe (Tippbuffer)", "100");
			await AssertFontWeightValueAsync("Punkte-Summe (Spielstand)", "700");
			await AssertFontWeightValueAsync("Aktuelle Kehre-Punkte", "700");

			// Cleanup
			await SelectFontWeightAsync("Eingabe (Tippbuffer)", "700");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			try
			{
				await SelectFontWeightAsync("Eingabe (Tippbuffer)", "700");
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
			catch { }
			throw;
		}
	}

	/// <summary>
	/// Test 6: Changing a font-weight setting actually renders — verifies the CSS-variable
	/// injection end-to-end by reading the computed style of the real element on /ziel, not
	/// just the persisted setting.
	/// </summary>
	[Fact]
	public async Task Phase11_FontWeightAppliesToRenderedCell()
	{
		LogPhaseStart(CurrentPhase, "Test 6: Schriftstärke wird auf gerenderter Zelle sichtbar");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			await SelectFontWeightAsync("Summe", "900");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			Log(CurrentPhase, "Navigiere zu /ziel");
			await Fixture.Page!.GotoAsync("http://localhost:5001/ziel");
			await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
			await Task.Delay(500);

			var summeCell = Fixture.Page.Locator(".ziel-summe").First;
			var computedWeight = await summeCell.EvaluateAsync<string>("el => getComputedStyle(el).fontWeight");

			Assert.Equal("900", computedWeight);
			Log(CurrentPhase, $"✓ Computed font-weight von .ziel-summe = {computedWeight}", LogSymbol.Check);

			// Cleanup: zurück zu /themes und zurücksetzen
			await NavigateToFontWeightTabAsync();
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			try
			{
				await NavigateToFontWeightTabAsync();
				await ClickResetAllButtonAsync();
				await Task.Delay(DEBOUNCE_DELAY_MS);
			}
			catch { }
			throw;
		}
	}
}
