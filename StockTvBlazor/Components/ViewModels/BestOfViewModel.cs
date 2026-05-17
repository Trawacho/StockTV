using StockTvBlazor.Services;
using StockTvBlazor.Settings;

namespace StockTvBlazor.Components.ViewModels;

public class BestOfViewModel(SettingsService settingsService, MatchService matchService) :
	BaseViewModel(settingsService, matchService)
{
	public string HeaderText => _isDemoMode
		? $"{HeaderTextBasis}   Spiel: {DemoData.SpielNummer}   Kehre: {DemoData.KehreAnzahl}"
		: (CurrentMatch.CurrentGame.GameNumber == 1 && CurrentMatch.CurrentGame.Turns.Count == 0)
			? HeaderTextBasis
			: $"{HeaderTextBasis}   Spiel: {CurrentMatch.CurrentGame.GameNumber}   Kehre: {CurrentMatch.CurrentGame.Turns.Count}";

	public new string LeftPoints => _isDemoMode
		? DemoData.LeftPoints
		: (base.CurrentMatch.CurrentGame.Turns.Count == 0 && base.CurrentMatch.CurrentGame.GameNumber > 1)
			? base.CurrentMatch.LeftPointsOverAll.ToString()
			: base.LeftPoints;

	public new string RightPoints => _isDemoMode
		? DemoData.RightPoints
		: (base.CurrentMatch.CurrentGame.Turns.Count == 0 && base.CurrentMatch.CurrentGame.GameNumber > 1)
			? base.CurrentMatch.RightPointsOverAll.ToString()
			: base.RightPoints;

	public int LeftMatchPoints => _isDemoMode ? DemoData.LeftMatchPoints : base.CurrentMatch.MatchPointsLeft;

	public int RightMatchPoints => _isDemoMode ? DemoData.RightMatchPoints : base.CurrentMatch.MatchPointsRight;
}
