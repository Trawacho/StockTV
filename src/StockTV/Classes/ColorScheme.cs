using System;
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

        public enum Schemes
        {
            Normal,
            Dark
        }

        #endregion


        #region Implementation of NotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propeertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propeertyName));
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
        /// <param name="scheme"></param>
        public ColorScheme(Schemes scheme = Schemes.Normal)
        {
            Scheme = scheme;
        }

        #endregion


        #region Functions

        /// <summary>
        /// Changes the current ColorScheme
        /// </summary>
        private void SwitchColorScheme()
        {
            Scheme = Scheme == Schemes.Normal
                              ? Schemes.Dark
                              : Schemes.Normal;
        }

        /// <summary>
        /// Loads the ColorScheme from local settings
        /// </summary>
        /// <returns></returns>
        internal static ColorScheme Load()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            var colorschema = localSettings.Values["ColorScheme"] as String;
            return new ColorScheme(colorschema.ToEnum<ColorScheme.Schemes>());
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


        #region Properties

        private Schemes schemes;

        /// <summary>
        /// ColorScheme
        /// </summary>
        internal Schemes Scheme
        {
            get
            {
                return schemes;
            }
            set
            {
                if (schemes == value)
                    return;

                schemes = value;
                NotifyPropertyChangedAllProperties();

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["ColorScheme"] = value.ToString();
            }
        }
        #endregion


        #region ReadOnly Properties

        /// <summary>
        /// Brush for the Header text
        /// </summary>
        public SolidColorBrush MainTextForeground
        {
            get
            {
                switch (Scheme)
                {
                    case Schemes.Normal:
                        return new SolidColorBrush(Colors.Black);
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.LightGray);
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
                switch (Scheme)
                {
                    case Schemes.Normal:
                        return new SolidColorBrush(Colors.White);
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.Black);
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
                switch (Scheme)
                {
                    case Schemes.Normal:
                        return new SolidColorBrush(Colors.Red);
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.Red);
                    default:
                        return new SolidColorBrush(Colors.Red);
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
                switch (Scheme)
                {
                    case Schemes.Normal:
                        return new SolidColorBrush(Colors.Green);
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.YellowGreen);
                    default:
                        return new SolidColorBrush(Colors.Green);
                }
            }
        }

       

        #endregion


    }
}
