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

	protected override void OnInitialized()
	{
		SettingsService.OnSettingsChanged += HandleSettingsChanged;
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
		var defaultColors = ColorSettingsFactory.FromTheme(UiSettings.Theme.Hell, UiSettings.Richtung.Links);
		_editingTheme = new CustomTheme
		{
			Name = "Mein Theme",
			Colors = CopyColors(defaultColors)
		};
	}

	private void EditTheme(CustomTheme theme)
	{
		_isNew = false;
		_editingTheme = new CustomTheme
		{
			Id = theme.Id,
			Name = theme.Name,
			Colors = CopyColors(theme.Colors)
		};
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
		ForegroundLeft = s.ForegroundLeft,
		ForegroundRight = s.ForegroundRight,
		ZielSummeGesamt = s.ZielSummeGesamt,
		ZielSummeEinzel = s.ZielSummeEinzel
	};
}
