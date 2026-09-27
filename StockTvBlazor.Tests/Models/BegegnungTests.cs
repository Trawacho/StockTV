using StockTvBlazor.Models;

namespace StockTvBlazor.Tests.Models;

public class BegegnungTests
{
	private const int TestSpielNummer = 1;
	private const string TeamA = "Alpha";
	private const string TeamB = "Beta";

	[Fact]
	public void TeamNameLeft_WithoutColorSchemeSwap_ReturnsTeamA()
	{
		var begegnung = new Begegnung(TestSpielNummer, TeamA, TeamB);

		var result = begegnung.TeamNameLeft(isColorSchemeRightToLeft: false);

		Assert.Equal(TeamA, result);
	}

	[Fact]
	public void TeamNameLeft_WithColorSchemeSwap_ReturnsTeamB()
	{
		var begegnung = new Begegnung(TestSpielNummer, TeamA, TeamB);

		var result = begegnung.TeamNameLeft(isColorSchemeRightToLeft: true);

		Assert.Equal(TeamB, result);
	}

	[Fact]
	public void TeamNameRight_WithoutColorSchemeSwap_ReturnsTeamB()
	{
		var begegnung = new Begegnung(TestSpielNummer, TeamA, TeamB);

		var result = begegnung.TeamNameRight(isColorSchemeRightToLeft: false);

		Assert.Equal(TeamB, result);
	}

	[Fact]
	public void TeamNameRight_WithColorSchemeSwap_ReturnsTeamA()
	{
		var begegnung = new Begegnung(TestSpielNummer, TeamA, TeamB);

		var result = begegnung.TeamNameRight(isColorSchemeRightToLeft: true);

		Assert.Equal(TeamA, result);
	}
}
