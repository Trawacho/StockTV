using Microsoft.AspNetCore.Components;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.Pages;

public partial class ThemePreview
{
	[Parameter, EditorRequired] public ColorSettings Colors { get; set; } = new();
}
