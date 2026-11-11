using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;


public class TrainingViewModel(SettingsService settingsSerivce, MatchService matchService) :
	BaseViewModel(settingsSerivce, matchService)
{
	public string HeaderText => _isDemoMode
		? $"{HeaderTextBasis}     Kehre: {DemoData.KehreAnzahl}"
		: $"{HeaderTextBasis}     Kehre: {LaufendeKehre}";

	public new string GetShellGridStyle() => "grid-template-columns: 100%;";
}
