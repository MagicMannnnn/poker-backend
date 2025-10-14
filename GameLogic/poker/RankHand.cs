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
                    best = Math.Max(best, getHighCard(setOfFive) + getPair(setOfFive) + getTOAK(setOfFive) + getStraight(setOfFive) + getFlush(setOfFive) + getFH(setOfFive) + getFOAK(setOfFive) + getStraightFlush(setOfFive));
                }
                scores[i] = best;
                Console.WriteLine(i + ": " + scores[i]);
            }

            float max = scores.Max();
            Console.WriteLine(max);
            int[] idx = scores.Select((v, i) => (v, i))
                         .Where(t => t.v == max)
                         .Select(t => t.i)
                         .ToArray();
            foreach (int index in idx)
            {
                Console.WriteLine(index);
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
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                for (int j = i + 1; j < cards.Length; j++)
                {
                    Card card2 = cards[j];
                    if (card.Value == card2.Value)
                    {
                        bestValue = Math.Max(bestValue, card.Value);
                    }
                }
            }
            return bestValue == 0 ? 0 : 10 + (float)(bestValue / 15f);
        }


        private static float getTOAK(Card[] cards)
        {
            int bestValue = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                for (int j = i + 1; j < cards.Length; j++)
                {
                    Card card2 = cards[j];
                    for (int k = j + 1; k < cards.Length; k++)
                    {
                        Card card3 = cards[k];
                        if (card.Value == card2.Value && card2.Value == card3.Value)
                        {
                            bestValue = Math.Max(bestValue, card.Value);
                        }
                    }
                }
            }
            return bestValue == 0 ? 0 : 100 + (float)(bestValue / 15f);
        }


        private static float getStraight(Card[] cards)
        {
            Array.Sort(cards, (a, b) => a.Value.CompareTo(b.Value));

            int prevValue = cards[0].Value;
            for (int i = 1; i < cards.Length; i++)
            {
                if (cards[i].Value == prevValue + 1)
                {
                    prevValue++;
                }
                else
                {
                    return 0;
                }
            }
            return 1000 + (float)(prevValue / 15f);
        }

        private static float getFlush(Card[] cards)
        {
            String prevSuit = cards[0].Suit;
            for (int i = 1; i < cards.Length; i++)
            {
                if (!(cards[i].Suit == prevSuit))
                {
                    return 0;
                }
            }
            Array.Sort(cards, (a, b) => b.Value.CompareTo(a.Value));
            return 10000 + (float)(cards[0].Value / 15f);
        }

        private static float getFH(Card[] cards)
        {
            var groups = cards
            .GroupBy(c => c.Value)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.Value)
            .ToArray();

            if (groups.Length == 2 && groups.Any(g => g.Count == 3) && groups.Any(g => g.Count == 2))
            {
                int three = groups.First(g => g.Count == 3).Value;
                int pair = groups.First(g => g.Count == 2).Value;

                // score however you like; example:
                return 1000000 + Math.Max(three, pair) / 15f;
            }
            return 0f;
        }


        private static float getFOAK(Card[] cards)
        {
            int bestValue = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                Card card = cards[i];
                for (int j = i + 1; j < cards.Length; j++)
                {
                    Card card2 = cards[j];
                    for (int k = j + 1; k < cards.Length; k++)
                    {
                        Card card3 = cards[k];
                        for (int l = k + 1; l < cards.Length; l++)
                        {
                            Card card4 = cards[l];
                            if (card.Value == card2.Value && card2.Value == card3.Value && card3.Value == card4.Value)
                            {
                                bestValue = Math.Max(bestValue, card.Value);
                            }
                        }
                    }
                }
            }
            return bestValue == 0 ? 0 : 1000000 + (float)(bestValue / 15f);
        }

        private static float getStraightFlush(Card[] cards)
        {
            if (getStraight(cards) > 0 && getFlush(cards) > 0)
            {
                return 10000000 + getStraight(cards);
            }
            return 0;
        }

    }

}