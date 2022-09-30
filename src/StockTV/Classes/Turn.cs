namespace StockTV.Classes
{
    public interface ITurn
    {
        int TurnNumber { get; set; }
        byte PointsRight { get; set; }
        byte PointsLeft { get; set; }
    }
    public class Turn : ITurn
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
        public byte PointsRight { get; set; } = 0;

        /// <summary>
        /// points int this turn for GREEN
        /// </summary>
        public byte PointsLeft { get; set; } = 0;
    }
}
