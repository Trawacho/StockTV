using StockTvBlazor.Components.Extensions;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;


namespace StockTvBlazor.Components.Models
{
	public class Settings
	{
		// Route or path that should be used as application start page.
		// When the server makes the navigation decision this value is used to redirect 
		// requests from the root URL to the configured page (e.g. "/training").
		public string StartPage { get; set; } = "/training";

		public Color BackgroundColor = Color.Black;
		public GameSettings GameSettings { get; set; } = new();
		public ColorScheme ColorScheme { get; set; } = new();

		public int MessageVersion { get; internal set; }
		public string SpielgruppeLetter { get; internal set; } = "";
		public bool BlockLocalChanges { get; internal set; }

		internal void PublishGameResult(object value)
		{
			//todo: implement function to publish game result
		}


		/// <summary>
		/// Changes the CourtNumber up or down
		/// </summary>
		/// <param name="up">true = increase, false = decrease</param>
		public void CourtNumberChange(bool up = true)
		{
			CourtNumber += up ? 1 : -1;
		}

		public void SpielgruppeChange(bool up = true)
		{
			Spielgruppe += up ? 1 : -1;
		}

		public void NetworkOnOffChange()
		{
			Networking = !Networking;
		}

		private bool _networking;
		public bool Networking
		{
			get => _networking;
			set
			{
				if (_networking == value) return;
				SetSaveProperty(ref _networking, value, nameof(Networking));
				if (value)
				{
					//todo: implement network connection
				}
			}
		}


		private int _spielgruppe;
		public int Spielgruppe
		{
			get { return _spielgruppe; }
			set
			{
				if (_spielgruppe == value ||
					value < 0 ||
					value > 10)
					return;

				SetSaveProperty(ref _spielgruppe, value, nameof(Spielgruppe));
			}
		}

		private int _courtNumber;
		/// <summary>
		/// Number of the Course
		/// </summary>
		public int CourtNumber
		{
			get { return _courtNumber; }
			set
			{
				if (_courtNumber == value ||
						  value < 1 ||
						  value > 99)
					return;

				SetSaveProperty(ref _courtNumber, value, nameof(CourtNumber));
			}
		}


		private bool SetSaveProperty<T>(ref T storage, T value, string propertyName)
		{
			if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

			storage = value;

			//todo: SetSaveProperty needs to be implemented
			//var localSettings = ApplicationData.Current.LocalSettings;
			//localSettings.Values[propertyName] = value.ToString();

			return true;
		}

		internal void PublishSettings()
		{
			//todo: implement function to publish settings 
		}
	}
	public class ColorScheme : INotifyPropertyChanged
	{
		#region Public Enumeration

		public enum ColorModis
		{
			Normal = 0,
			Dark = 1
		}
		public enum NextBahnModis
		{
			Left = 0,
			Right = 1
		}

		#endregion


		#region Implementation of NotifyPropertyChanged

		public event PropertyChangedEventHandler? PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		private void NotifyPropertyChangedAllProperties()
		{
			foreach (var item in this.GetType().GetProperties())
			{
				OnPropertyChanged(item.Name);
			}
		}

		#endregion


		#region Constructor

		/// <summary>
		/// Default-Constructor
		/// </summary>
		/// <param name="colorModus"></param>
		public ColorScheme(ColorModis colorModus = ColorModis.Normal, NextBahnModis nextBahn = NextBahnModis.Left)
		{
			ColorModus = colorModus;
			NextBahnModus = nextBahn;
		}

		#endregion


		#region Functions

		/// <summary>
		/// Changes the current ColorScheme
		/// </summary>
		private void SwitchColorScheme()
		{
			ColorModus = ColorModus == ColorModis.Normal
							  ? ColorModis.Dark
							  : ColorModis.Normal;
		}

		/// <summary>
		/// Changes the current Direction
		/// </summary>
		private void SwitchRightToLeft()
		{
			NextBahnModus = NextBahnModus == NextBahnModis.Left ? NextBahnModis.Right : NextBahnModis.Left;
		}

		/// <summary>
		/// Loads the ColorScheme from local settings
		/// </summary>
		/// <returns></returns>
		internal static ColorScheme Load()
		{
			//todo: Load needs to be implemented
			// var localSettings = ApplicationData.Current.LocalSettings;

			var colorschema = "1";  // localSettings.Values[nameof(ColorModus)] as string;
			var nextbahn = "1";     // localSettings.Values[nameof(NextBahnModus)] as string;

			return new ColorScheme(
				colorschema.ToEnum<ColorScheme.ColorModis>(),
				nextbahn.ToEnum<ColorScheme.NextBahnModis>());
		}


		internal void ColorSchemeUp()
		{
			SwitchColorScheme();
		}

		internal void ColorSchemeDown()
		{
			SwitchColorScheme();

		}
		#endregion

		internal void RightToLeftUp()
		{
			SwitchRightToLeft();
		}

		internal void RightToLeftDown()
		{
			SwitchRightToLeft();
		}

		#region Properties

		private ColorModis _colormodus;

		/// <summary>
		/// ColorScheme
		/// </summary>
		internal ColorModis ColorModus
		{
			get => _colormodus;
			set => SetSaveProperty(ref _colormodus, value, nameof(ColorModus));
		}

		internal void SetColorModus(byte value)
		{
			var e = Enum.Parse<ColorModis>(value.ToString());
			ColorModus = e;
		}

		private NextBahnModis _nextbahnmodus;
		/// <summary>
		/// Next Bahn Left or Right
		/// </summary>
		internal NextBahnModis NextBahnModus
		{
			get => _nextbahnmodus;
			set => SetSaveProperty(ref _nextbahnmodus, value, nameof(NextBahnModus));
		}

		internal void SetNextBahnModus(byte value)
		{
			var e = Enum.Parse<NextBahnModis>(value.ToString());
			NextBahnModus = e;
		}
		#endregion

		public byte[] AsByteArray()
		{
			return System.Text.Encoding.UTF8.GetBytes(ColorModus.ToString());
		}

		#region ReadOnly Properties

		/// <summary>
		/// Brush for the Header text
		/// </summary>
		public Color MainTextForeground
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Dark => Color.LightGray,
					_ => Color.Black,
				};
			}

		}

		/// <summary>
		/// Brush for the Background
		/// </summary>
		public Color MainBackground
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Dark => Color.Black,
					_ => Color.White
				};
			}
		}

		/// <summary>
		/// Brush for the left side
		/// </summary>
		public Color LeftForeground
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Dark => NextBahnModus == NextBahnModis.Left
												 ? Color.Red
												: Color.YellowGreen,
					_ => NextBahnModus == NextBahnModis.Left
												? Color.Red
												: Color.Green
				};
			}
		}

		/// <summary>
		/// Brush for the right side
		/// </summary>
		public Color RightForeground
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Dark => NextBahnModus == NextBahnModis.Left
												? Color.YellowGreen
												: Color.Red,
					_ => NextBahnModus == NextBahnModis.Left
												? Color.Green
												: Color.Red
				};
			}
		}

		public Color ZielSummeGesamtForeGround
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Normal => Color.DarkMagenta,
					_ => Color.Magenta
				};
			}
		}

		public Color ZielSummeEinzelForeGround
		{
			get
			{
				return ColorModus switch
				{
					ColorModis.Normal => Color.DarkCyan,
					_ => Color.Cyan,
				};
			}
		}


		#endregion

		private bool SetSaveProperty<T>(ref T storage, T value, string propertyName) where T : notnull
		{
			if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

			storage = value;

			NotifyPropertyChangedAllProperties();
			//todo: SetSaveProperty needs to be implemented
			//var localSettings = ApplicationData.Current.LocalSettings;
			//localSettings.Values[propertyName] = value.ToString();

			return true;
		}

	}

}
