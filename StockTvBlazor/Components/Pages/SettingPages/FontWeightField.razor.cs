using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace StockTvBlazor.Components.Pages.SettingPages;

public partial class FontWeightField
{
	[Parameter, EditorRequired]
	public string Label { get; set; } = "";

	[Parameter]
	public double Value { get; set; }

	[Parameter]
	public EventCallback<double> ValueChanged { get; set; }

	[Parameter]
	public double DefaultValue { get; set; }

	private string DefaultDisplay => DefaultValue.ToString("0", CultureInfo.InvariantCulture);

	private async Task OnValueChanged() => await ValueChanged.InvokeAsync(Value);
}
