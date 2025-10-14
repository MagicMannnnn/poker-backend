namespace PokerServer.GameLogic.poker
{
    public class RankHand
    {
        public static Player? winner {get; set; }
        private static List<Card> _board;
        public static List<Player> getwinners(List<Player> players, List<Card> board)
        {

            if (winner != null)
            {
                List<Player> winners = new List<Player>();
                winners.Add(winner);
                return winners;
            }

            _board = board;

            float[] highCardScores = new float[players.Count];

            for (int i = 0; i < players.Count; i++)
            {
                //highCardScores[i] = players[i].score;
            }

            return players;
        }
        

        private static float getHighCard()
        {
            return 0;
        }
    }

}