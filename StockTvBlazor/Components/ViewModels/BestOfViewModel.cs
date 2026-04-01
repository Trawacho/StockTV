using StockTvBlazor.Components.Models;
using StockTvBlazor.Components.Networking;
using StockTvBlazor.Components.Services;

namespace StockTvBlazor.Components.ViewModels
{
	public class BestOfViewModel(SettingsService settingsService, MatchService matchService, NetMqPublisherService publisherService) :
		BaseViewModel(settingsService, matchService, publisherService)
	{
		public string HeaderText => (CurrentMatch.CurrentGame.GameNumber == 1 && CurrentMatch.CurrentGame.Turns.Count == 0)
			? HeaderTextBasis
			: $"{HeaderTextBasis}   Spiel: {CurrentMatch.CurrentGame.GameNumber}   Kehre: {CurrentMatch.CurrentGame.Turns.Count}";

		public new string LeftPoints =>
			(base.CurrentMatch.CurrentGame.Turns.Count == 0 && base.CurrentMatch.CurrentGame.GameNumber > 1)
				? base.CurrentMatch.LeftPointsOverAll.ToString()
				: base.LeftPoints;

		public new string RightPoints =>
			(base.CurrentMatch.CurrentGame.Turns.Count == 0 && base.CurrentMatch.CurrentGame.GameNumber > 1)
				? base.CurrentMatch.RightPointsOverAll.ToString()
				: base.RightPoints;

		public int LeftMatchPoints => base.CurrentMatch.MatchPointsLeft;

		public int RightMatchPoints => base.CurrentMatch.MatchPointsRight;
	}
}