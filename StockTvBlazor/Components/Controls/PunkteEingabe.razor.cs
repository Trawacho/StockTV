using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace StockTvBlazor.Components.Controls;

public partial class PunkteEingabe
{
	[Parameter] public string LeftPoints { get; set; } = "";
	[Parameter] public string RightPoints { get; set; } = "";
	[Parameter] public string InputValue { get; set; } = "0";
	[Parameter] public EventCallback<string> OnWertChanged { get; set; }
	[Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

	private ElementReference inputRef;
	private bool IstErlaubteTaste = true;

	private static readonly string[] ErlaubteTasten =
	[
		"0","1","2","3","4","5","6","7","8","9",
		"Numpad0","Numpad1","Numpad2","Numpad3","Numpad4",
		"Numpad5","Numpad6","Numpad7","Numpad8","Numpad9",
		"Add", "Subtract", "Decimal",
		"+", "-", ".", ",",
		"Backspace","Delete","Tab",
		"ArrowLeft","ArrowRight","ArrowUp","ArrowDown",
		"Home","End"
	];

	public async Task SetFocusAsync()
	{
		await inputRef.FocusAsync();
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (System.Diagnostics.Debugger.IsAttached)
			Console.WriteLine($"Key pressed: {e.Key}");

		IstErlaubteTaste = ErlaubteTasten.Contains(e.Key);

		if (IstErlaubteTaste)
			await OnKeyDown.InvokeAsync(e);
		else
		{
			InputValue = "0";
			StateHasChanged();
		}
	}
}
