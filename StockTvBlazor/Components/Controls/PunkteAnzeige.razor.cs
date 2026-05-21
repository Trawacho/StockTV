using Microsoft.AspNetCore.Components;

namespace StockTvBlazor.Components.Controls;

public partial class PunkteAnzeige
{
	[Parameter] public int LeftPointsSum { get; set; }
	[Parameter] public int RightPointsSum { get; set; }
}
