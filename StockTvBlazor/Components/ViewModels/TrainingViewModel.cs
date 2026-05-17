using StockTvBlazor.Services;

namespace StockTvBlazor.Components.ViewModels;


public class TrainingViewModel(SettingsService settingsSerivce, MatchService matchService) :
	BaseViewModel(settingsSerivce, matchService)
{
	public string HeaderText => CurrentMatch.CurrentGame.Turns.Count == 0
		? base.HeaderTextBasis
		: $"{HeaderTextBasis}     Kehre: {CurrentMatch.CurrentGame.Turns.Count}";

		public new string GetShellGridStyle() => "grid-template-columns: 100%;";
}
