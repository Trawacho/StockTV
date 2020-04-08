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
        public enum Schemes
        {
            Normal,
            Dark
        }

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


        public ColorScheme(Schemes scheme = Schemes.Normal)
        {
            Scheme = scheme;
        }

        #endregion


        #region Functions

        internal void SwitchColorScheme()
        {
            Scheme = Scheme == Schemes.Normal
                              ? Schemes.Dark
                              : Schemes.Normal;
        }

        internal static ColorScheme Load()
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            var colorschema = localSettings.Values["ColorScheme"] as String;
            return new ColorScheme(colorschema.ToEnum<ColorScheme.Schemes>());
        }

        #endregion


        #region Properties

        private Schemes schemes;

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

        public SolidColorBrush HeaderForeground
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

        public SolidColorBrush ColonForeground
        {
            get
            {
                return HeaderForeground;
            }
        }

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

        public SolidColorBrush InputValueForeground
        {
            get
            {
                return HeaderForeground;
            }
        }

        #endregion


    }
}
