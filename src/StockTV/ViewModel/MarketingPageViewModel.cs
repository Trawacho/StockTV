using StockTV.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;

namespace StockTV.ViewModel
{
    public class MarketingPageViewModel : BaseViewModel
    {

        #region BaseViewModel implementation

        /// <summary>
        /// Returns only the <see cref="Settings"/>, no result
        /// </summary>
        /// <returns></returns>
        internal override byte[] GetSerializedResult()
        {
            return Settings.GetSettings();
        }

        /// <summary>
        /// no action 
        /// </summary>
        /// <param name="begegnungen"></param>
        internal override void SetTeamNames(string begegnungen)
        {
            return;
        }

        /// <summary>
        /// <inheritdoc/><br>no action</br>
        /// </summary>
        /// <param name="teilnehmer"></param>
        internal override void SetTeilnehmer(string teilnehmer)
        {
            return;
        }

        /// <summary>
        /// Depending on <see cref="GameSettings.GameModus"/> the page get displayed
        /// </summary>
        internal override void SetMatchReset()
        {
            return;
        }

        /// <summary>
        /// Set settings 
        /// </summary>
        /// <param name="settings"></param>
        internal override void SetSettings(byte[] settings)
        {
            Settings.SetSettings(settings);
        }



        #endregion

        #region Constructor

        public MarketingPageViewModel() : base()
        {
            DefaultImage = new BitmapImage()
            {
                UriSource = new Uri(GetMediaFiles("Assets/Logo").First())
            };
        }

        #endregion

        internal void GetScanCode(uint scanCode)
        {
            /*
             * ScanCode of KeyPad
             * ScanCode: 69 NumberKeyLock
             * ScanCode: 82 0
             * ScanCode: 79 1
             * ScanCode: 80 2
             * ScanCode: 81 3
             * ScanCode: 75 4
             * ScanCode: 76 5
             * ScanCode: 77 6
             * ScanCode: 71 7
             * ScanCode: 72 8
             * ScanCode: 73 9
             * ScanCode: 53 /                   --> ROT
             * ScanCode: 55 *                   --> GRÜN
             * ScanCode: 74 -                   --> BLAU
             * ScanCode: 78 +                   --> GELB
             * ScanCode: 28 Enter
             * ScanCode: 83 ,
             * ScanCode: 14 BackSpace           --> ROT
             *
             */

            if (scanCode == 74 || scanCode == 78)
                NavigateTo(typeof(Pages.MarketingPage));
        }

        /// <summary>
        /// Default Picture in Project
        /// </summary>
        private BitmapImage DefaultImage;

        /// <summary>
        /// Picture from Settings or DefaultImage
        /// </summary>
        public BitmapImage MarketingImage
        {
            get
            {
                return Settings.MarketingImage ?? DefaultImage;
            }
        }


        private IEnumerable<string> GetMediaFiles(string rootPath)
        {
            string folderPath = Path.Combine(
                    Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                    rootPath.Replace('/', '\\').TrimStart('\\')
                );
            return Directory.GetFiles(folderPath, "*.png").AsEnumerable();
        }

    }
}
