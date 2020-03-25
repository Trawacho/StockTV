using StockTV.Classes;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StockTV.ViewModel
{
    public class MainPageViewModel
    {
        /// <summary>
        /// UWP ViewModel
        /// </summary>

        #region NotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        #endregion


        public void Do(uint ScanCode)
        {
            System.Diagnostics.Debug.WriteLine($"ScanCode: {ScanCode} ");

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
             * ScanCode: 53 /
             * ScanCode: 55 *
             * ScanCode: 74 -
             * ScanCode: 78 +
             * ScanCode: 28 Enter
             * ScanCode: 83 ,
             * ScanCode: 14 BackSpace
             *
             */

            switch (ScanCode)
            {
                case 69:    // NumLock

                case 83:    // ,
                case 28:    // Enter
                    break;

                case 55:    // *
                    MyWertung.AddRight();
                    break;

                case 74:    // -
                    MyWertung.DeleteLastTurn();
                    break;

                case 53:    // /
                case 14:    // BackSpace
                    MyWertung.AddLeft();
                    break;

                case 78:    // +
                    MyWertung.Reset();
                    break;


                #region Numbers 1 to 0

                case 79:
                    //MyWertung.InputText = "1";
                    MyWertung.AddInput(1);
                    break;
                case 80:
                    //MyWertung.InputText = "2";
                    MyWertung.AddInput(2);
                    break;
                case 81:
                    //MyWertung.InputText = "3";
                    MyWertung.AddInput(3);
                    break;
                case 75:
                    //MyWertung.InputText = "4";
                    MyWertung.AddInput(4);
                    break;
                case 76:
                    //MyWertung.InputText = "5";
                    MyWertung.AddInput(5);
                    break;
                case 77:
                    //MyWertung.InputText = "6";
                    MyWertung.AddInput(6);
                    break;
                case 71:
                    //MyWertung.InputText = "7";
                    MyWertung.AddInput(7);
                    break;
                case 72:
                    //MyWertung.InputText = "8";
                    MyWertung.AddInput(8);
                    break;
                case 73:
                    //MyWertung.InputText = "9";
                    MyWertung.AddInput(9);
                    break;
                case 82:
                    //MyWertung.InputText = "0";
                    MyWertung.AddInput(0);
                    break;

                    #endregion
            }


        }

        public Result MyWertung { get; set; }


        public MainPageViewModel()
        {
            MyWertung = new Result(30, 30);
        }

    }
}
