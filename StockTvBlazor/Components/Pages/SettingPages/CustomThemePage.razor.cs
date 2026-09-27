using Microsoft.AspNetCore.Components;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class CustomThemePage : IDisposable
{
	[Inject] private SettingsService SettingsService { get; set; } = default!;

	private CustomTheme? _editingTheme;
	private bool _isNew;
	private string _errorMessage = "";
	private ColorSettings? _previewColors;
	private string _previewName = "";
	private bool _disposed;
	private int _baseThemeValue = -1;
	private bool _showColorSettings = false;
	private bool _showAnspielSettings = false;
	private string _activeTab = "themes";

	private void SetTab(string tab)
	{
		_activeTab = tab;
	}

	private void ShowThemesTab() => SetTab("themes");
	private void ShowLayoutTab() => SetTab("layout");
	private void ShowFontWeightTab() => SetTab("fontweight");

	protected override void OnInitialized()
	{
		SettingsService.OnSettingsChanged += HandleSettingsChanged;
		var activeTheme = SettingsService.CurrentSettings.UI.ActiveTheme;
		SelectTheme(activeTheme);
	}

	public void Dispose()
	{
		_disposed = true;
		SettingsService.OnSettingsChanged -= HandleSettingsChanged;
	}

	private void HandleSettingsChanged()
	{
		if (_disposed) return;
		InvokeAsync(StateHasChanged);
	}

	private void SelectTheme(ITheme theme)
	{
		SettingsService.ActivateTheme(theme.Id);

		if (theme.IsBuiltIn)
		{
			_editingTheme = null;
			_previewColors = theme.GetColors(SettingsService.CurrentSettings.UI.CurrentRichtung);
			_previewName = theme.Name;
		}
		else
		{
			_previewColors = null;
			EditTheme((CustomTheme)theme);
		}
	}

	private void CreateNewTheme()
	{
		_isNew = true;
		_previewColors = null;
		var activeTheme = SettingsService.CurrentSettings.UI.ActiveTheme;
		var templateColors = activeTheme.GetColors(SettingsService.CurrentSettings.UI.CurrentRichtung);
		UiSettings.Theme? baseTheme = activeTheme is BuiltInTheme builtin ? builtin.ThemeType : null;
		_editingTheme = new CustomTheme
		{
			Name = "Mein Theme",
			Colors = CopyColors(templateColors),
			BaseTheme = baseTheme
		};
		_baseThemeValue = baseTheme.HasValue ? (int)baseTheme.Value : -1;
	}

	private void EditTheme(CustomTheme theme)
	{
		_isNew = false;
		_editingTheme = new CustomTheme
		{
			Id = theme.Id,
			Name = theme.Name,
			Colors = CopyColors(theme.Colors),
			BaseTheme = theme.BaseTheme,
			ShowFrameOnAnspiel = theme.ShowFrameOnAnspiel,
			FrameWidthPx = theme.FrameWidthPx
		};
		_baseThemeValue = theme.BaseTheme.HasValue ? (int)theme.BaseTheme.Value : -1;
	}

	private void SaveTheme()
	{
		if (_editingTheme is null) return;

		if (string.IsNullOrWhiteSpace(_editingTheme.Name))
		{
			_errorMessage = "Bitte einen Namen angeben.";
			return;
		}

		var savedId = _editingTheme.Id;
		SettingsService.AddOrUpdateCustomTheme(_editingTheme);
		SettingsService.ActivateTheme(savedId);

		_isNew = false;
		_errorMessage = "";
	}

	private void CancelEdit()
	{
		_editingTheme = null;
		_errorMessage = "";
		_baseThemeValue = -1;
		_showColorSettings = false;
		_showAnspielSettings = false;
	}

	/// <summary>
	/// Wendet Änderungen an einem bestehenden Theme sofort an (persistiert + published).
	/// Bei einem neuen Theme (noch nicht gespeichert) tut diese Methode bewusst nichts —
	/// dort bleibt "Speichern" der einzige Weg, das Theme überhaupt anzulegen.
	/// </summary>
	private void ApplyLiveChangeIfExisting()
	{
		if (_isNew || _editingTheme is null) return;
		if (string.IsNullOrWhiteSpace(_editingTheme.Name)) return;

		SettingsService.AddOrUpdateCustomTheme(_editingTheme);
	}

	private void ToggleColorSettings()
	{
		_showColorSettings = !_showColorSettings;
	}

	private void ToggleAnspielSettings()
	{
		_showAnspielSettings = !_showAnspielSettings;
	}

	private void OnBaseThemeChanged(ChangeEventArgs e)
	{
		if (_editingTheme is null) return;

		if (int.TryParse(e.Value?.ToString(), out var value) && value >= 0)
		{
			_baseThemeValue = value;
			_editingTheme.BaseTheme = (UiSettings.Theme)value;
		}
		else
		{
			_baseThemeValue = -1;
			_editingTheme.BaseTheme = null;
		}

		ApplyLiveChangeIfExisting();
	}

	private void DeleteTheme(Guid id)
	{
		SettingsService.DeleteCustomTheme(id);
		if (_editingTheme?.Id == id)
			_editingTheme = null;
	}

	private static ColorSettings CopyColors(ColorSettings s) => new()
	{
		BackgroundColor = s.BackgroundColor,
		ForegroundColor = s.ForegroundColor,
		ForegroundA = s.ForegroundA,
		ForegroundB = s.ForegroundB,
		TeamNameA = s.TeamNameA,
		TeamNameB = s.TeamNameB,
		ZielSummeGesamt = s.ZielSummeGesamt,
		ZielSummeEinzel = s.ZielSummeEinzel,
		ZielSpielername = s.ZielSpielername
	};
}
