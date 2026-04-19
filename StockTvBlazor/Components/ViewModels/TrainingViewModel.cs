using StockTvBlazor.Services;
using StockTvBlazor.Networking;

namespace StockTvBlazor.Components.ViewModels;


public class TrainingViewModel(SettingsService settingsSerivce, MatchService matchService) :
	BaseViewModel(settingsSerivce, matchService)
{
	public string HeaderText => CurrentMatch.CurrentGame.Turns.Count == 0
		? base.HeaderTextBasis
		: $"{HeaderTextBasis}     Kehre: {CurrentMatch.CurrentGame.Turns.Count}";
}
