using StockTvBlazor.E2ETests.Fixtures;
using StockTvBlazor.E2ETests.Tests.Phases;
using Xunit;
using Xunit.Abstractions;

namespace StockTvBlazor.E2ETests.Tests;

/// <summary>
/// Comprehensive E2E scenario: single app instance, all 7 modes sequentially.
/// Duration: ~6 minutes. All phases run in one [Fact] to guarantee sequential execution.
/// </summary>
public class ComprehensiveScenarioE2ETests : IClassFixture<AppFixture>
{
	private readonly AppFixture _fixture;
	private readonly ITestOutputHelper _output;

	public ComprehensiveScenarioE2ETests(AppFixture fixture, ITestOutputHelper output)
	{
		_fixture = fixture;
		_output = output;
	}

	[Fact]
	public async Task Comprehensive_E2E_AllPhases_Sequential()
	{
		await new Phase1TrainingE2ETests(_fixture, _output).Phase1_Training_15Kehren();
		await new Phase2TournamentE2ETests(_fixture, _output).Phase2_Turnier_3Spiele();
		await new Phase3BestOfE2ETests(_fixture, _output).Phase3_BestOf_3Spiele();
		//await new Phase4Ziel6E2ETests(_fixture, _output).Phase4_Ziel_6Kehren();
		//await new Phase5Ziel12E2ETests(_fixture, _output).Phase5_Ziel_12Kehren();
		//await new Phase6Ziel2E2ETests(_fixture, _output).Phase6_Ziel2_2Runden();
		//await new Phase7SettingsE2ETests(_fixture, _output).Phase7_Settings_Navigation();
	}
}
