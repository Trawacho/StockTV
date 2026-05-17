using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class ColorField
{
	[Parameter, EditorRequired]
	public string Label { get; set; } = "";

	[Parameter]
	public string Value { get; set; } = "#000000";

	[Parameter]
	public EventCallback<string> ValueChanged { get; set; }

	private async Task OnColorInput(ChangeEventArgs e)
	{
		var val = e.Value?.ToString() ?? "#000000";
		await ValueChanged.InvokeAsync(val);
	}

	private async Task OnTextInput(ChangeEventArgs e)
	{
		var val = e.Value?.ToString() ?? "";
		if (Regex.IsMatch(val, @"^#[0-9a-fA-F]{6}$"))
			await ValueChanged.InvokeAsync(val);
	}
}
