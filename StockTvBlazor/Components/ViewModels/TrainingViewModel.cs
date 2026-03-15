using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;

namespace StockTvBlazor.Components.ViewModels
{

	public class TrainingViewModel : BaseViewModel
	{
		private int _inputValue;


		public TrainingViewModel(Models.Settings settings, NavigationManager navigationManager) : base(settings, navigationManager)
		{
			_inputValue = -1;
			Match.TurnsChanged += Match_TurnsChanged;
		}

		private void Match_TurnsChanged(object? sender, EventArgs e)
		{
			if (sender is Models.Match m)
			{
				//todo: implement broadcasting
				if (_settings.MessageVersion == 0)
				{
					_settings.PublishGameResult(m.Serialize());
				}
				else if (_settings.MessageVersion == 1)
				{
					_settings.PublishGameResult(m.SerializeJson());
				}
			}
		}


		public string HeaderText
		{
			get
			{
				if (Match.CurrentGame.GameNumber == 1 &&
					Match.CurrentGame.Turns.Count == 0)
				{
					return $"{(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.CourtNumber}";
				}
				else
				{
					return $"{(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.CourtNumber}   Kehre: {Match.CurrentGame.Turns.Count}";
				}
			}
		}
		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();
		
	}
}
