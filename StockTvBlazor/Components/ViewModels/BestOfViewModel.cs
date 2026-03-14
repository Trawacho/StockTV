using Microsoft.AspNetCore.Components;
using StockTvBlazor.Components.Models;

namespace StockTvBlazor.Components.ViewModels
{
	public class BestOfViewModel : BaseViewModel
	{
		private int _inputValue;


		public BestOfViewModel(Settings settings, NavigationManager navigationManager) : base(settings, navigationManager)
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
					if (_settings.SpielgruppeLetter == string.Empty)
						return $"{(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.CourtNumber}";
					else
						return $"{(_settings.BlockLocalChanges ? "." : "")}Bahn: {_settings.SpielgruppeLetter}-{_settings.CourtNumber}";
				}

				return $"B {(_settings.BlockLocalChanges ? "." : "")}Spiel: {Match.CurrentGame.GameNumber}     Kehre: {Match.CurrentGame.Turns.Count}";
			}
		}

		public int LeftPointsSum => base.Match.CurrentGame.LeftPointsSum;
		public int RightPointsSum => base.Match.CurrentGame.RightPointsSum;
		public string LeftPoins => base.Match.CurrentGame.LeftPoints;
		public string RightPoints => base.Match.CurrentGame.RightPoints;
		public new string InputValue => base.InputValue < 0 ? "" : base.InputValue.ToString();
		public void AddInput(int value)
		{
			if (_inputValue < 0)
			{
				if (value <= _settings.GameSettings.PointsPerTurn)
					_inputValue = value;
			}
			else if ((_inputValue * 10) + value <= _settings.GameSettings.PointsPerTurn)
			{
				_inputValue = Convert.ToSByte((_inputValue * 10) + value);
			}
			else
			{
				if (value <= _settings.GameSettings.PointsPerTurn)
					_inputValue = value;
				else
					_inputValue = -1;
			}
		}
	}
}