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
        public ColorScheme(Schemes scheme = Schemes.Normal, bool rightToLeft = true)
        {
            Scheme = scheme;
            RightToLeft = rightToLeft;
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
        /// Changes the current Direction
        /// </summary>
        private void SwitchRightToLeft()
        {
            RightToLeft = !RightToLeft;
        }

        /// <summary>
        /// Loads the ColorScheme from local settings
        /// </summary>
        /// <returns></returns>
        internal static ColorScheme Load()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            var colorschema = localSettings.Values["ColorScheme"] as string;
            var righttoleft = localSettings.Values["RightToLeft"] as string;
            return new ColorScheme(
                colorschema.ToEnum<ColorScheme.Schemes>(),
                Convert.ToBoolean(righttoleft)
                );
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

        private bool rightToLeft;

        /// <summary>
        /// RightToLeft Direction
        /// </summary>
        internal bool RightToLeft
        {
            get
            {
                return rightToLeft;
            }

            set
            {
                if (rightToLeft == value)
                    return;
                rightToLeft = value;
                NotifyPropertyChangedAllProperties();

                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.Values["RightToLeft"] = value.ToString();
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
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.LightGray);
                    case Schemes.Normal:
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
                    case Schemes.Dark:
                        return new SolidColorBrush(Colors.Black);
                    case Schemes.Normal:
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

                    case Schemes.Dark:
                        return RightToLeft
                            ? new SolidColorBrush(Colors.Red)
                            : new SolidColorBrush(Colors.YellowGreen);

                    case Schemes.Normal:
                    default:
                        return RightToLeft
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
                switch (Scheme)
                {

                    case Schemes.Dark:
                        return RightToLeft
                            ? new SolidColorBrush(Colors.YellowGreen)
                            : new SolidColorBrush(Colors.Red);

                    case Schemes.Normal:
                    default:
                        return RightToLeft
                            ? new SolidColorBrush(Colors.Green)
                            : new SolidColorBrush(Colors.Red);
                }
            }
        }



        #endregion


    }
}
