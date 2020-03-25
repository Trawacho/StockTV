using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StockTV.Classes
{
    public class Result : INotifyPropertyChanged
    {
        /// <summary>
        /// Manages the Results of a game.
        /// </summary>

        int maxValueInput = 30;             // maximum Value per Turn
        int maxCountTurns = 30;             // maximum Count of Turns
        bool _isAddingAllowed = false;       // controls if Adding the Value from Turn is allowed

        #region NotifyPropertyChanged implementation

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange([CallerMemberName] string propertyname = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));
        }

        /// <summary>
        /// Notify about changes for all properties
        /// </summary>
        private void allPropertiesChanged()
        {
            RaisePropertyChange(nameof(this.Left));
            RaisePropertyChange(nameof(this.Right));
            RaisePropertyChange(nameof(this.LeftTurns));
            RaisePropertyChange(nameof(this.RightTurns));
            RaisePropertyChange(nameof(this.HeaderText));
            RaisePropertyChange(nameof(this.InputText));
            RaisePropertyChange(nameof(this.IsAddingAllowed));
        }


        #endregion

        #region private fields / properties

        private List<Turn> turns = new List<Turn>();

        private int _inputValue;


        #endregion

        #region Constructor

        /// <summary>
        /// Default-Constructor
        /// </summary>
        public Result()
        {
            this.turns = new List<Turn>();

        }

        /// <summary>
        /// Parameterized constructor
        /// </summary>
        /// <param name="maxValueInput">maximum possible Value per turn</param>
        /// <param name="maxCountTurns">maximun of Turns per result</param>
        public Result(int maxValueInput, int maxCountTurns) : this()
        {
            this.maxCountTurns = maxCountTurns;
            this.maxValueInput = maxValueInput;
        }

        #endregion

        #region private Functions
        private void AddTurnToList(Turn turn)
        {

            if (_isAddingAllowed && turns.Count < maxCountTurns)
            {
                turns.Add(turn);
                _isAddingAllowed = false;
            }
            _inputValue = 0;

            allPropertiesChanged();

        }

        private string GetTurnsString(bool fromLeft = true)
        {
            string temp = string.Empty;
            if (TurnNumber > 0)
            {
                foreach (var item in turns.OrderBy(x => x.TurnNumber))
                {
                    temp += String.IsNullOrEmpty(temp) ? "" : "-";
                    temp += fromLeft ? $"{item.PointsLeft}" : $"{item.PointsRight}";
                }
            }
            return temp;
        }

        #endregion

        #region public Functions

        /// <summary>
        /// Add Value to intermidate value
        /// </summary>
        /// <param name="value"></param>
        public void AddInput(int value)
        {
            _inputValue = _inputValue == 0 ? value : (_inputValue * 10) + value;

            _isAddingAllowed = true;

            if (_inputValue > maxValueInput)
            {
                _inputValue = 0;
                _isAddingAllowed = false;
            }

            allPropertiesChanged();
        }

        /// <summary>
        /// Add Points to the Right
        /// </summary>
        /// <param name="value"></param>
        public void AddRight()
        {
            AddTurnToList(new Turn()
            {
                TurnNumber = turns.Count + 1,
                PointsRight = _inputValue
            });
        }

        /// <summary>
        /// Add Points to the Left
        /// </summary>
        /// <param name="value"></param>
        public void AddLeft()
        {
            AddTurnToList(new Turn()
            {
                TurnNumber = turns.Count + 1,
                PointsLeft = _inputValue,
            });
        }

        /// <summary>
        /// delete the last turn
        /// </summary>
        public void DeleteLastTurn()
        {
            if (turns.Count > 0)
            {
                turns.RemoveAt(turns.Count - 1);
                _inputValue = 0;
                allPropertiesChanged();
            }
        }

        /// <summary>
        /// Reset the Result 
        /// </summary>
        public void Reset()
        {
            turns.Clear();
            _inputValue = 0;
            allPropertiesChanged();
        }

        #endregion

        #region Read-Only Properties
        /// <summary>
        /// Number of turns in Result
        /// </summary>
        public int TurnNumber
        {
            get
            {
                return turns.Count;
            }
        }

        /// <summary>
        /// Information for Header 
        /// </summary>
        public string HeaderText
        {
            get
            {
                return TurnNumber > 0 ? $"Spielstand nach Kehre {TurnNumber}" : $"Spielstand";
            }
        }

        /// <summary>
        /// Text about all turns from REDs
        /// </summary>
        public string RightTurns
        {
            get
            {
                return GetTurnsString(false);
            }
        }

        /// <summary>
        /// Text aboutn all turns from GREENs
        /// </summary>
        public string LeftTurns
        {
            get
            {
                return GetTurnsString(true);
            }
        }

        /// <summary>
        /// sum of points for the Right
        /// </summary>
        public string Right
        {
            get
            {
                return turns.Sum(w => w.PointsRight).ToString();
            }
        }

        /// <summary>
        /// sum of points for the Left
        /// </summary>
        public string Left
        {
            get
            {
                return turns.Sum(w => w.PointsLeft).ToString();

            }
        }

        /// <summary>
        /// shows text (value) received from input as preInformation
        /// </summary>
        public string InputText
        {
            get
            {
                return _inputValue.ToString();
            }
        }

        /// <summary>
        /// TRUE if Input can be added to left or right
        /// </summary>
        public bool IsAddingAllowed
        {
            get
            {
                return _isAddingAllowed;
            }
        }
        #endregion
    }
}
