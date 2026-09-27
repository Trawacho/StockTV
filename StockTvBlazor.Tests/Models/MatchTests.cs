using Microsoft.Extensions.Logging.Abstractions;
using StockTvBlazor.Models;
using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Tests.Models;

public class MatchTests
{
	private static async Task<Match> CreateInitializedMatchAsync()
	{
		var settingsService = new SettingsService(NullLogger<SettingsService>.Instance, new FileLoggerProvider());
		await settingsService.InitializeAsync();
		var persistence = new GameStatePersistenceService(NullLogger<GameStatePersistenceService>.Instance);
		return new Match(settingsService, NullLogger<MatchService>.Instance, persistence);
	}

	[Fact]
	public async Task ClearThenAddBegegnungen_ReplacesPriorEntry_ForRepeatedSave()
	{
		var match = await CreateInitializedMatchAsync();
		match.AddBegegnung(new Begegnung(1, "Alt A", "Alt B"));

		match.ClearBegegnungen();
		match.AddBegegnung(new Begegnung(1, "Neu A", "Neu B"));

		var begegnung = Assert.Single(match.Begegnungen);
		Assert.Equal(1, begegnung.Spielnummer);
		Assert.Equal("Neu A", begegnung.TeamNameLeft(isColorSchemeRightToLeft: false));
	}

	[Fact]
	public async Task ClearThenAddBegegnungen_AlsoRemovesEntriesOutsideOneToSeven()
	{
		var match = await CreateInitializedMatchAsync();
		match.AddBegegnung(new Begegnung(8, "Alt A", "Alt B"));

		match.ClearBegegnungen();
		for (int spielNummer = 1; spielNummer <= 7; spielNummer++)
			match.AddBegegnung(new Begegnung(spielNummer, "Team A", "Team B"));

		Assert.DoesNotContain(match.Begegnungen, b => b.Spielnummer == 8);
	}

	[Fact]
	public async Task ClearThenAddBegegnungen_DirectionRechts_MapsLeftFieldToTeamA()
	{
		var match = await CreateInitializedMatchAsync();
		const bool isLinks = false; // Richtung.Rechts
		const string leftFieldValue = "Team Links";
		const string rightFieldValue = "Team Rechts";

		var teamA = isLinks ? rightFieldValue : leftFieldValue;
		var teamB = isLinks ? leftFieldValue : rightFieldValue;

		match.ClearBegegnungen();
		for (int spielNummer = 1; spielNummer <= 7; spielNummer++)
			match.AddBegegnung(new Begegnung(spielNummer, teamA, teamB));

		var begegnung = match.Begegnungen.First(b => b.Spielnummer == 1);
		Assert.Equal(leftFieldValue, begegnung.TeamNameLeft(isLinks));
		Assert.Equal(rightFieldValue, begegnung.TeamNameRight(isLinks));
	}

	[Fact]
	public async Task ClearThenAddBegegnungen_DirectionLinks_MapsLeftFieldToTeamB()
	{
		var match = await CreateInitializedMatchAsync();
		const bool isLinks = true; // Richtung.Links
		const string leftFieldValue = "Team Links";
		const string rightFieldValue = "Team Rechts";

		var teamA = isLinks ? rightFieldValue : leftFieldValue;
		var teamB = isLinks ? leftFieldValue : rightFieldValue;

		match.ClearBegegnungen();
		for (int spielNummer = 1; spielNummer <= 7; spielNummer++)
			match.AddBegegnung(new Begegnung(spielNummer, teamA, teamB));

		var begegnung = match.Begegnungen.First(b => b.Spielnummer == 1);
		Assert.Equal(leftFieldValue, begegnung.TeamNameLeft(isLinks));
		Assert.Equal(rightFieldValue, begegnung.TeamNameRight(isLinks));
	}

	[Fact]
	public async Task ClearThenAddBegegnungen_CreatesGamesOneThroughSeven()
	{
		var match = await CreateInitializedMatchAsync();

		match.ClearBegegnungen();
		for (int spielNummer = 1; spielNummer <= 7; spielNummer++)
			match.AddBegegnung(new Begegnung(spielNummer, "Team A", "Team B"));

		Assert.Equal(Enumerable.Range(1, 7), match.Begegnungen.Select(b => b.Spielnummer));
	}
}
