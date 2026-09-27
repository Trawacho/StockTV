using Microsoft.Playwright;
using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Helpers;
using Xunit;
using Xunit.Abstractions;
using System.Text.Json;
using System.Globalization;

namespace StockTvBlazor.E2ETests.Tests.Phases;

[Collection("E2E Sequential")]
public class Phase9ThemeLayoutE2ETests : PhaseTestBase
{
	public Phase9ThemeLayoutE2ETests(AppFixture fixture, ITestOutputHelper output)
		: base(fixture, output)
	{
		CurrentPhase = "Phase 9";
	}

	#region PRIVATE HELPERS

	/// <summary>
	/// Navigates to /themes and clicks the "Tabellenstruktur" tab.
	/// </summary>
	private async Task NavigateToTableLayoutTabAsync()
	{
		if (Fixture.Page == null)
			throw new InvalidOperationException("Page is null");

		Log(CurrentPhase, "Navigiere zu /themes");
		await Fixture.Page.GotoAsync("http://localhost:5001/themes");
		await Fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
		await Task.Delay(500);

		Log(CurrentPhase, "Klicke auf Tab 'Tabellenstruktur'");
		var tabButton = Fixture.Page.Locator(".tab-btn", new() { HasTextString = "Tabellenstruktur" });
		await tabButton.ClickAsync();
		await Task.Delay(500);
	}

	/// <summary>
	/// Opens an accordion group if it's currently collapsed and returns the group's
	/// ".accordion-item" locator, so callers can scope further field lookups to it
	/// (fields are not uniquely labeled across groups, e.g. "Mitte" exists in several).
	/// </summary>
	private async Task<ILocator> OpenAccordionGroupAsync(string groupLabel)
	{
		if (Fixture.Page == null)
			throw new InvalidOperationException("Page is null");

		var groupItem = Fixture.Page.Locator(".accordion-item", new() { HasTextString = groupLabel });
		var button = groupItem.Locator(".accordion-button");
		var isCollapsed = await button.EvaluateAsync<bool>("el => el.classList.contains('collapsed')");

		if (isCollapsed)
		{
			Log(CurrentPhase, $"Öffne Accordion-Gruppe: {groupLabel}");
			await button.ClickAsync();
			await Task.Delay(300);
		}

		return groupItem;
	}


	/// <summary>
	/// Fills a layout field with a value and triggers blur (to trigger @bind-value:after).
	/// Uses XPath to find the input within the layout-field div that contains the label,
	/// scoped to the given accordion group (labels like "Links"/"Mitte"/"Rechts" repeat
	/// across groups, so an unscoped page-wide search can match the wrong group's field).
	/// </summary>
	private async Task FillLayoutFieldAsync(ILocator group, string fieldLabel, string value)
	{
		Log(CurrentPhase, $"Setze {fieldLabel} = {value}");
		// Leading "." makes the XPath relative to `group` instead of the whole document.
		var xpathExpr = $"xpath=.//div[contains(@class,'layout-field')][.//label[contains(.,'{fieldLabel}')]]//input[@type='number']";
		var input = group.Locator(xpathExpr).First;

		await input.FillAsync(value);
		await input.PressAsync("Tab"); // Blur to trigger @bind-value:after
		await Task.Delay(100);
	}

	/// <summary>
	/// Gets the current value of a layout field using XPath locator, scoped to the given
	/// accordion group (see FillLayoutFieldAsync for why scoping is required).
	/// </summary>
	private async Task<string> GetLayoutFieldValueAsync(ILocator group, string fieldLabel)
	{
		var xpathExpr = $"xpath=.//div[contains(@class,'layout-field')][.//label[contains(.,'{fieldLabel}')]]//input[@type='number']";
		var input = group.Locator(xpathExpr).First;
		var value = await input.InputValueAsync();
		return value ?? "";
	}

	/// <summary>
	/// Clicks the "Alle Werte zurücksetzen" button.
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

	/// <summary>
	/// Validates a layout field has the expected value.
	/// </summary>
	private async Task AssertLayoutFieldValueAsync(ILocator group, string fieldLabel, string expectedValue)
	{
		var actualValue = await GetLayoutFieldValueAsync(group, fieldLabel);
		var actual = double.Parse(actualValue, CultureInfo.InvariantCulture);
		var expected = double.Parse(expectedValue, CultureInfo.InvariantCulture);

		Assert.True(Math.Abs(actual - expected) < 0.01,
			$"Field '{fieldLabel}' should be {expectedValue}, but got {actualValue}");

		Log(CurrentPhase, $"✓ Feld {fieldLabel} = {actualValue} (erwartet: {expectedValue})", LogSymbol.Check);
	}

	#endregion PRIVATE HELPERS

	/// <summary>
	/// Test 1: Default values display correctly (regression test for @bind-value bug).
	/// Opens Kehre-Zeile group and verifies the three inputs show 42.5 / 15 / 42.5.
	/// First resets all values to ensure we're starting from defaults.
	/// </summary>
	[Fact]
	public async Task Phase9_KehreZeile_DefaultValuesDisplayCorrectly()
	{
		LogPhaseStart(CurrentPhase, "Test 1: Standard-Werte korrekt angezeigt");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToTableLayoutTabAsync();
			// Reset all values first to ensure we start with defaults
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS);

			var kehreZeile = await OpenAccordionGroupAsync("Kehre-Zeile");

			// Verify default values: 42.5 / 15 / 42.5
			await AssertLayoutFieldValueAsync(kehreZeile, "Links", "42.5");
			await AssertLayoutFieldValueAsync(kehreZeile, "Mitte", "15");
			await AssertLayoutFieldValueAsync(kehreZeile, "Rechts", "42.5");

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
	/// Test 2: Sum warning appears when sum != 100%, disappears when fixed.
	/// Changes a value to trigger invalid sum, checks warning display, then fixes it.
	/// </summary>
	[Fact]
	public async Task Phase9_SumWarning_AppearsWhenSumNot100_DisappearsWhenFixed()
	{
		LogPhaseStart(CurrentPhase, "Test 2: Summen-Warnung bei ungültiger Summe");
		Assert.NotNull(Fixture.Page);

		ILocator? kehreZeileGroup = null;
		try
		{
			await NavigateToTableLayoutTabAsync();
			kehreZeileGroup = await OpenAccordionGroupAsync("Kehre-Zeile");

			// Change Links to 50 → sum becomes 107.5, should show warning
			await FillLayoutFieldAsync(kehreZeileGroup, "Links", "50");
			await Task.Delay(DEBOUNCE_DELAY_MS); // Wait for parent re-render

			var warning = kehreZeileGroup.Locator(".sum-warning");

			var isVisible = await warning.IsVisibleAsync();
			Assert.True(isVisible, "Sum warning should be visible when sum != 100%");

			var warningText = await warning.TextContentAsync();
			Assert.NotNull(warningText);
			Assert.Contains("107.5", warningText); // Sum: 50 + 15 + 42.5
			Log(CurrentPhase, $"✓ Warnung angezeigt: {warningText?.Trim()}", LogSymbol.Check);

			// Fix it: set Links back to 42.5
			await FillLayoutFieldAsync(kehreZeileGroup, "Links", "42.5");
			await Task.Delay(DEBOUNCE_DELAY_MS); // Wait for parent re-render

			var warningCount = await warning.CountAsync();
			Assert.Equal(0, warningCount); // Warning should be gone (sum is now 100 again)
			Log(CurrentPhase, "✓ Warnung verschwunden nach Korrektur", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			// Cleanup
			if (kehreZeileGroup != null)
				await FillLayoutFieldAsync(kehreZeileGroup, "Links", "42.5");
			throw;
		}
	}

	/// <summary>
	/// Test 3: Value change persists to config file.
	/// Changes a value, waits for debounce, then validates it's in stocktv.config.json.
	/// </summary>
	[Fact]
	public async Task Phase9_ValueChange_PersistsToConfigFile()
	{
		LogPhaseStart(CurrentPhase, "Test 3: Wert-Änderung wird persistiert");
		Assert.NotNull(Fixture.Page);

		ILocator? punkteGrid = null;
		try
		{
			await NavigateToTableLayoutTabAsync();
			punkteGrid = await OpenAccordionGroupAsync("Punkte-Grid (Training / Turnier)");

			// Change MidGrid3MidWidth to 8
			const double newValue = 8;
			await FillLayoutFieldAsync(punkteGrid, "Mitte", newValue.ToString());
			await Task.Delay(DEBOUNCE_DELAY_MS); // Wait for debounce save

			Log(CurrentPhase, "Validiere Persistierung in stocktv.config.json...");
			var configJson = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
			{
				try
				{
					using var doc = JsonDocument.Parse(json);
					var root = doc.RootElement;
					var ui = root.GetProperty("UI");
					var tableLayout = ui.GetProperty("TableLayout");
					var midGrid3MidWidth = tableLayout.GetProperty("MidGrid3MidWidth").GetDouble();
					return Math.Abs(midGrid3MidWidth - newValue) < 0.01;
				}
				catch
				{
					return false;
				}
			});

			using var finalDoc = JsonDocument.Parse(configJson);
			var midGrid3Mid = finalDoc.RootElement
				.GetProperty("UI")
				.GetProperty("TableLayout")
				.GetProperty("MidGrid3MidWidth")
				.GetDouble();

			Assert.True(Math.Abs(midGrid3Mid - newValue) < 0.01);
			Log(CurrentPhase, $"✓ MidGrid3MidWidth = {midGrid3Mid} in Datei persistiert", LogSymbol.Check);

			// Cleanup: reset to default (6)
			await FillLayoutFieldAsync(punkteGrid, "Mitte", "6");
			await Task.Delay(DEBOUNCE_DELAY_MS);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			// Cleanup attempt
			try
			{
				if (punkteGrid != null)
				{
					await FillLayoutFieldAsync(punkteGrid, "Mitte", "6");
					await Task.Delay(DEBOUNCE_DELAY_MS);
				}
			}
			catch { }
			throw;
		}
	}

	/// <summary>
	/// Test 4: Reset all button restores defaults in UI and file.
	/// Changes multiple values, clicks reset, validates UI and config file.
	/// </summary>
	[Fact]
	public async Task Phase9_ResetAllValues_RestoresDefaultsInUiAndFile()
	{
		LogPhaseStart(CurrentPhase, "Test 4: 'Alle Werte zurücksetzen' stellt Defaults wieder her");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToTableLayoutTabAsync();

			// Change a few values
			var kehreZeile = await OpenAccordionGroupAsync("Kehre-Zeile");
			await FillLayoutFieldAsync(kehreZeile, "Links", "50");

			var punkteGrid = await OpenAccordionGroupAsync("Punkte-Grid (Training / Turnier)");
			await FillLayoutFieldAsync(punkteGrid, "Links", "45");

			// Click reset button
			await ClickResetAllButtonAsync();
			await Task.Delay(DEBOUNCE_DELAY_MS); // Wait for debounce save

			// Validate UI: Kehre-Zeile should be back to defaults
			kehreZeile = await OpenAccordionGroupAsync("Kehre-Zeile");
			await AssertLayoutFieldValueAsync(kehreZeile, "Links", "42.5");
			await AssertLayoutFieldValueAsync(kehreZeile, "Mitte", "15");
			await AssertLayoutFieldValueAsync(kehreZeile, "Rechts", "42.5");

			// Validate no warnings on page
			var allWarnings = await Fixture.Page.Locator(".sum-warning").CountAsync();
			Assert.Equal(0, allWarnings);
			Log(CurrentPhase, "✓ Keine Summen-Warnungen mehr sichtbar", LogSymbol.Check);

			// Validate config file
			Log(CurrentPhase, "Validiere config.json nach Reset...");
			var configJson = await Fixture.ReadLocalFileAsync("stocktv.config.json", json =>
			{
				try
				{
					using var doc = JsonDocument.Parse(json);
					var root = doc.RootElement;
					var ui = root.GetProperty("UI");

					// Check BottomGridLeftWidth (Kehre-Zeile Links) = 42.5
					var tableLayout = ui.GetProperty("TableLayout");
					var bottomLeft = tableLayout.GetProperty("BottomGridLeftWidth").GetDouble();
					var bottomMid = tableLayout.GetProperty("BottomGridMidWidth").GetDouble();
					var bottomRight = tableLayout.GetProperty("BottomGridRightWidth").GetDouble();

					// Check MidColumnWidth = 90 (default)
					var midColWidth = ui.GetProperty("MidColumnWidth").GetDouble();

					return Math.Abs(bottomLeft - 42.5) < 0.01
						&& Math.Abs(bottomMid - 15) < 0.01
						&& Math.Abs(bottomRight - 42.5) < 0.01
						&& Math.Abs(midColWidth - 90) < 0.01;
				}
				catch
				{
					return false;
				}
			});

			using var finalDoc = JsonDocument.Parse(configJson);
			var bottomGridLeft = finalDoc.RootElement
				.GetProperty("UI")
				.GetProperty("TableLayout")
				.GetProperty("BottomGridLeftWidth")
				.GetDouble();

			Assert.True(Math.Abs(bottomGridLeft - 42.5) < 0.01);
			Log(CurrentPhase, "✓ Config-Datei zeigt Default-Werte nach Reset", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			// Cleanup attempt: click reset
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
	/// Test 5: Group kind hints distinguish rows from columns.
	/// Opens a row group and a column group, validates the hint text.
	/// </summary>
	[Fact]
	public async Task Phase9_GroupKindHint_DistinguishesRowsFromColumns()
	{
		LogPhaseStart(CurrentPhase, "Test 5: Zeilen/Spalten-Hinweise unterscheiden Rows/Cols");
		Assert.NotNull(Fixture.Page);

		try
		{
			await NavigateToTableLayoutTabAsync();

			// Open a row group (e.g., "Kopf-/Mitte-/Fuß-Zeilen")
			var rowGroupItem = await OpenAccordionGroupAsync("Kopf-/Mitte-/Fuß-Zeilen");
			var rowHint = rowGroupItem.Locator(".group-kind");
			var rowHintText = await rowHint.TextContentAsync();

			Assert.NotNull(rowHintText);
			Assert.Contains("Zeilenhöhen", rowHintText);
			Log(CurrentPhase, $"✓ Zeilen-Hinweis korrekt: {rowHintText?.Trim()}", LogSymbol.Check);

			// Open a column group (e.g., "Kehre-Zeile")
			var colGroupItem = await OpenAccordionGroupAsync("Kehre-Zeile");
			var colHint = colGroupItem.Locator(".group-kind");
			var colHintText = await colHint.TextContentAsync();

			Assert.NotNull(colHintText);
			Assert.Contains("Spaltenbreiten", colHintText);
			Log(CurrentPhase, $"✓ Spalten-Hinweis korrekt: {colHintText?.Trim()}", LogSymbol.Check);

			LogPhaseEnd(CurrentPhase);
		}
		catch (Exception ex)
		{
			Log(CurrentPhase, $"Test fehlgeschlagen: {ex.Message}", LogSymbol.Error);
			throw;
		}
	}
}
