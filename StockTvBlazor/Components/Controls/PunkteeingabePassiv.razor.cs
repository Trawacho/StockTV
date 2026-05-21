using Microsoft.AspNetCore.Components;

namespace StockTvBlazor.Components.Controls;

public partial class PunkteeingabePassiv
{
	[Parameter] public string LeftPoints { get; set; } = "0";
	[Parameter] public string InputValue { get; set; } = "";
	[Parameter] public string RightPoints { get; set; } = "0";
	[Parameter] public string HilfeText { get; set; } = "Zahl eingeben und / oder * drücken";
}
