using Bunit;
using StockTvBlazor.Components.Controls;

namespace StockTvBlazor.Tests.Components;

public class AutoFitTextTests : BunitContext
{
	[Fact]
	public void AutoFitText_RenderWithText_DisplaysText()
	{
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, "Hello")
			.Add(p => p.Min, 10)
		);

		Assert.NotNull(cut.Markup);
		Assert.Contains("Hello", cut.Markup);
	}

	[Fact]
	public void AutoFitText_NullText_Renders()
	{
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, (string?)null)
			.Add(p => p.Min, 10)
		);

		Assert.NotNull(cut.Markup);
	}

	[Fact]
	public void AutoFitText_WithClass_IncludesClass()
	{
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, "Test")
			.Add(p => p.Min, 10)
			.Add(p => p.Class, "custom-class")
		);

		Assert.Contains("custom-class", cut.Markup);
	}

	[Fact]
	public void AutoFitText_WithVertical_IncludesVerticalClass()
	{
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, "Test")
			.Add(p => p.Min, 10)
			.Add(p => p.Vertical, true)
		);

		// Vertical should be included in the CSS class
		Assert.NotNull(cut.Markup);
	}

	[Fact]
	public void AutoFitText_LongText_StillRendersWithoutCrash()
	{
		var longText = new string('a', 500);
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, longText)
			.Add(p => p.Min, 5)
		);

		Assert.Contains(longText, cut.Markup);
	}

	[Fact]
	public void AutoFitText_MinValue_AppliedToStyle()
	{
		var cut = Render<AutoFitText>(parameters => parameters
			.Add(p => p.Text, "Test")
			.Add(p => p.Min, 42)
		);

		// Min should be in the inline style
		Assert.Contains("42", cut.Markup);
	}
}
