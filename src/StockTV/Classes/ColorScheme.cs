using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace StockTV.Classes
{
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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propeertyName = null)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(propeertyName));
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
            var localSettings = ApplicationData.Current.LocalSettings;

            var colorschema = localSettings.Values[nameof(ColorModus)] as string;
            var nextbahn = localSettings.Values[nameof(NextBahnModus)] as string;

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

        private ColorModis colormodus;

        /// <summary>
        /// ColorScheme
        /// </summary>
        internal ColorModis ColorModus
        {
            get => colormodus;
            set => SetSaveProperty(ref colormodus, value, nameof(ColorModus));
        }

        internal void SetColorModus(byte value)
        {
            var e = (ColorModis)Enum.Parse(typeof(ColorModis), value.ToString());
            ColorModus = e;
        }

        private NextBahnModis nextbahnmodus;
        /// <summary>
        /// Next Bahn Left or Right
        /// </summary>
        internal NextBahnModis NextBahnModus
        {
            get => nextbahnmodus;
            set => SetSaveProperty(ref nextbahnmodus, value, nameof(NextBahnModus));
        }

        internal void SetNextBahnModus(byte value)
        {
            var e = (NextBahnModis)Enum.Parse(typeof(NextBahnModis), value.ToString());
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
        public SolidColorBrush MainTextForeground
        {
            get
            {
                switch (ColorModus)
                {
                    case ColorModis.Dark:
                        return new SolidColorBrush(Colors.LightGray);
                    case ColorModis.Normal:
                    default:
                        return new SolidColorBrush(Colors.Black);
                }
            }

        }

        /// <summary>
        /// Brush for the Background
        /// </summary>
        public SolidColorBrush MainBackground
        {
            get
            {
                switch (ColorModus)
                {
                    case ColorModis.Dark:
                        return new SolidColorBrush(Colors.Black);
                    case ColorModis.Normal:
                    default:
                        return new SolidColorBrush(Colors.White);
                }
            }
        }

        /// <summary>
        /// Brush for the left side
        /// </summary>
        public SolidColorBrush LeftForeground
        {
            get
            {
                switch (ColorModus)
                {

                    case ColorModis.Dark:
                        return NextBahnModus == NextBahnModis.Left
                             ? new SolidColorBrush(Colors.Red)
                            : new SolidColorBrush(Colors.YellowGreen);
                    case ColorModis.Normal:
                    default:
                        return NextBahnModus == NextBahnModis.Left
                            ? new SolidColorBrush(Colors.Red)
                            : new SolidColorBrush(Colors.Green);
                }
            }
        }

        /// <summary>
        /// Brush for the right side
        /// </summary>
        public SolidColorBrush RightForeground
        {
            get
            {
                switch (ColorModus)
                {

                    case ColorModis.Dark:
                        return NextBahnModus == NextBahnModis.Left
                            ? new SolidColorBrush(Colors.YellowGreen)
                            : new SolidColorBrush(Colors.Red);

                    case ColorModis.Normal:
                    default:
                        return NextBahnModus == NextBahnModis.Left
                            ? new SolidColorBrush(Colors.Green)
                            : new SolidColorBrush(Colors.Red);
                }
            }
        }

        public SolidColorBrush ZielSummeGesamtForeGround
        {
            get
            {
                switch (ColorModus)
                {
                    case ColorModis.Normal:
                        return new SolidColorBrush(Colors.DarkMagenta);

                    case ColorModis.Dark:
                    default:
                        return new SolidColorBrush(Colors.Magenta);
                }
            }
        }

        public SolidColorBrush ZielSummeEinzelForeGround
        {
            get
            {
                switch (ColorModus)
                {
                    case ColorModis.Normal:
                        return new SolidColorBrush(Colors.DarkCyan);

                    case ColorModis.Dark:
                    default:
                        return new SolidColorBrush(Colors.Cyan);
                }
            }
        }


        #endregion

        private bool SetSaveProperty<T>(ref T storage, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value)) return false;

            storage = value;

            NotifyPropertyChangedAllProperties();

            var localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[propertyName] = value.ToString();

            return true;
        }

    }
}
