namespace PokerServer.GameLogic.poker
{
    public class RankHand
    {
        public static Player? winner { get; set; }
        private static List<Card> _board;
        public static List<Player> getwinners(List<Player> players, List<Card> board)
        {
            List<Player> winners = new List<Player>();
            if (winner != null)
            {
                winners.Add(winner);
                return winners;
            }

            _board = board;

            float[] scores = new float[players.Count];

            for (int i = 0; i < players.Count; i++)
            {
                List<Card> cards = new List<Card>();
                cards.AddRange(_board);
                cards.AddRange(players[i].realhand);
                float best = 0;
                foreach (var setOfFive in ComboUtil.KCombinations(cards, 5))
                {
                    best = Math.Max(best, getHighCard(setOfFive) + getPair(setOfFive));
                }
                scores[i] = best;
            }

            float max = scores.Max();
            int[] idx = scores.Select((v, i) => (v, i))
                         .Where(t => t.v == max)
                         .Select(t => t.i)
                         .ToArray();
            foreach (int index in idx)
            {
                winners.Add(players[index]);
            }

            return winners;
        }


        private static float getHighCard(Card[] cards)
        {
            int bestValue = 0;
            foreach (Card card in cards)
            {
                bestValue = Math.Max(bestValue, card.Value);
            }
            return (float)(bestValue / 15f);
        }

        private static float getPair(Card[] cards)
        {
            int bestValue = 0;
            foreach (Card card in cards)
            {
                foreach (Card card2 in cards)
                {
                    if (card.Value == card2.Value)
                    {
                        bestValue = Math.Max(bestValue, card.Value);
                    }
                }
            }
            return (float)(bestValue * 10 / 15f);
        }
    }

}