namespace StockTV.Classes
{
    public class Turn
    {
        /// <summary>
        /// Default-Constructor
        /// </summary>
        public Turn()
        {

        }

        /// <summary>
        /// number of turn
        /// </summary>
        public int TurnNumber { get; set; }

        /// <summary>
        /// points in this turn for RED
        /// </summary>
        public int PointsRight { get; set; } = 0;

        /// <summary>
        /// points int this turn for GREEN
        /// </summary>
        public int PointsLeft { get; set; } = 0;
    }
}
