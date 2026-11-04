using System.Text.Json;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Settings;

public class AnspielFrameSettingsTests
{
	[Fact]
	public void Defaults_FrameOn_3px()
	{
		var frame = new UiSettings().AnspielFrame;

		Assert.True(frame.ShowFrame);
		Assert.Equal(3, frame.FrameWidthPx);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(-5, 1)]
	[InlineData(4, 4)]
	[InlineData(50, 10)]
	public void EffectiveWidthPx_ClampsToAllowedRange(int stored, int expected)
	{
		var frame = new AnspielFrameSettings { FrameWidthPx = stored };

		Assert.Equal(expected, frame.EffectiveWidthPx);
	}

	[Fact]
	public void Migrate_WithoutLegacyValues_ChangesNothing()
	{
		var ui = new UiSettings();
		ui.CustomThemes.Add(new CustomTheme { Name = "Neu" });

		Assert.False(ui.MigrateLegacyAnspielFrame());
		Assert.True(ui.AnspielFrame.ShowFrame);
		Assert.Equal(3, ui.AnspielFrame.FrameWidthPx);
	}

	[Fact]
	public void Migrate_TakesWidthOfActiveThemeWithFrame()
	{
		var other = new CustomTheme { Name = "Andere", LegacyShowFrameOnAnspiel = true, LegacyFrameWidthPx = 7 };
		var active = new CustomTheme { Name = "Aktiv", LegacyShowFrameOnAnspiel = true, LegacyFrameWidthPx = 5 };
		var ui = new UiSettings();
		ui.CustomThemes.AddRange([other, active]);
		ui.ActivateTheme(active.Id);

		Assert.True(ui.MigrateLegacyAnspielFrame());

		Assert.True(ui.AnspielFrame.ShowFrame);
		Assert.Equal(5, ui.AnspielFrame.FrameWidthPx);
	}

	[Fact]
	public void Migrate_ThemesWithoutFrame_KeepDefaultFrameOn()
	{
		// Alte Dateien enthalten bei jedem eigenen Theme "ShowFrameOnAnspiel": false - das war die
		// alte Vorgabe, keine bewusste Abwahl. Der Rahmen ist jetzt standardmaessig an.
		var ui = new UiSettings();
		ui.CustomThemes.Add(new CustomTheme { Name = "Alt", LegacyShowFrameOnAnspiel = false, LegacyFrameWidthPx = 8 });

		Assert.True(ui.MigrateLegacyAnspielFrame());

		Assert.True(ui.AnspielFrame.ShowFrame);
		Assert.Equal(3, ui.AnspielFrame.FrameWidthPx);
	}

	[Fact]
	public void Migrate_ClearsLegacyValues_SecondCallDoesNothing()
	{
		var theme = new CustomTheme { Name = "Alt", LegacyShowFrameOnAnspiel = true, LegacyFrameWidthPx = 6 };
		var ui = new UiSettings();
		ui.CustomThemes.Add(theme);

		ui.MigrateLegacyAnspielFrame();

		Assert.Null(theme.LegacyShowFrameOnAnspiel);
		Assert.Null(theme.LegacyFrameWidthPx);
		Assert.False(ui.MigrateLegacyAnspielFrame());
	}

	[Fact]
	public void Json_OldThemeFile_IsMigrated_AndLegacyFieldsDisappear()
	{
		const string oldJson = """
			{
			  "ActiveThemeId": "11111111-1111-1111-1111-111111111111",
			  "CustomThemes": [
			    {
			      "Id": "11111111-1111-1111-1111-111111111111",
			      "Name": "Halle",
			      "ShowFrameOnAnspiel": true,
			      "FrameWidthPx": 6
			    }
			  ]
			}
			""";

		var ui = JsonSerializer.Deserialize<UiSettings>(oldJson)!;

		// Fehlt "AnspielFrame" in der Datei, gilt die neue Vorgabe.
		Assert.True(ui.AnspielFrame.ShowFrame);

		ui.MigrateLegacyAnspielFrame();
		Assert.Equal(6, ui.AnspielFrame.FrameWidthPx);

		var newJson = JsonSerializer.Serialize(ui);
		Assert.DoesNotContain("ShowFrameOnAnspiel", newJson);
		Assert.Contains("\"AnspielFrame\":{\"ShowFrame\":true,\"FrameWidthPx\":6}", newJson);
	}

	[Fact]
	public void Json_SavedFrameOff_StaysOff()
	{
		var ui = new UiSettings();
		ui.AnspielFrame.ShowFrame = false;

		var roundTrip = JsonSerializer.Deserialize<UiSettings>(JsonSerializer.Serialize(ui))!;

		Assert.False(roundTrip.AnspielFrame.ShowFrame);
	}
}
