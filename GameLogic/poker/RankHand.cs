
using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerServer.GameLogic.poker
{
    public enum HandCategory
    {
        HighCard = 0,
        Pair = 1,
        TwoPair = 2,
        ThreeKind = 3,
        Straight = 4,
        Flush = 5,
        FullHouse = 6,
        FourKind = 7,
        StraightFlush = 8
    }

    public sealed class HandRank : IComparable<HandRank>
    {
        public HandCategory Category { get; }
        public int[] Ranks { get; } // tie-breakers in descending priority (e.g., for pair: pair rank, kickers...)
        public Player Player { get; }

        public HandRank(HandCategory cat, IEnumerable<int> ranks, Player player)
        {
            Category = cat;
            Ranks = ranks.ToArray();
            Player = player;
        }

        public int CompareTo(HandRank? other)
        {
            if (other is null) return 1;
            int c = Category.CompareTo(other.Category);
            if (c != 0) return c;
            // Lexicographic compare of rank arrays
            int len = Math.Max(Ranks.Length, other.Ranks.Length);
            for (int i = 0; i < len; i++)
            {
                int a = i < Ranks.Length ? Ranks[i] : 0;
                int b = i < other.Ranks.Length ? other.Ranks[i] : 0;
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }
    }

    public static class RankHand
    {
        public static Player? winner { get; set; }

        public static List<Player> getwinners(List<Player> players, List<Card> board)
        {
            // If there's exactly one active player (everyone else folded) the caller sets winner.
            if (winner != null)
                return new List<Player> { winner };

            var result = new List<(HandRank rank, Player player)>();
            foreach (var p in players)
            {
                var seven = new List<Card>(board);
                seven.AddRange(p.realhand);

                HandRank best = null!;
                foreach (var five in ComboUtil.KCombinations(seven, 5))
                {
                    var hr = Evaluate5(five, p);
                    if (best == null || hr.CompareTo(best) > 0)
                        best = hr;
                }
                result.Add((best, p));
            }

            // Find best hand(s)
            var bestRank = result.Max(t => t.rank);
            var winners = result.Where(t => t.rank.CompareTo(bestRank) == 0)
                                .Select(t => t.player)
                                .ToList();
            return winners;
        }

        private static HandRank Evaluate5(Card[] cards, Player player)
        {
            // map to values and suits
            var values = cards.Select(c => c.Value).OrderByDescending(v => v).ToArray();
            var suits = cards.Select(c => c.Suit).ToArray();

            bool isFlush = suits.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1;
            // For straight, handle wheel (A-2-3-4-5)
            bool isStraight;
            int topStraight;
            (isStraight, topStraight) = IsStraight(cards);

            if (isStraight && isFlush)
                return new HandRank(HandCategory.StraightFlush, new[] { topStraight }, player);

            // group by value
            var groups = cards.GroupBy(c => c.Value)
                              .Select(g => new { Value = g.Key, Count = g.Count() })
                              .OrderByDescending(g => g.Count)
                              .ThenByDescending(g => g.Value)
                              .ToList();

            if (groups[0].Count == 4)
            {
                // Four of a kind
                int four = groups[0].Value;
                int kicker = groups.First(g => g.Count == 1).Value;
                return new HandRank(HandCategory.FourKind, new[] { four, kicker }, player);
            }

            if (groups[0].Count == 3 && groups[1].Count == 2)
            {
                // Full house
                int trips = groups[0].Value;
                int pair = groups[1].Value;
                return new HandRank(HandCategory.FullHouse, new[] { trips, pair }, player);
            }

            if (isFlush)
            {
                // Flush: top five high cards
                var ordered = cards.Select(c => c.Value).OrderByDescending(v => v).ToArray();
                return new HandRank(HandCategory.Flush, ordered, player);
            }

            if (isStraight)
                return new HandRank(HandCategory.Straight, new[] { topStraight }, player);

            if (groups[0].Count == 3)
            {
                int trips = groups[0].Value;
                var kickers = groups.Where(g => g.Count == 1).Select(g => g.Value).OrderByDescending(v => v);
                return new HandRank(HandCategory.ThreeKind, (new[] { trips }).Concat(kickers), player);
            }

            if (groups[0].Count == 2 && groups[1].Count == 2)
            {
                int highPair = Math.Max(groups[0].Value, groups[1].Value);
                int lowPair = Math.Min(groups[0].Value, groups[1].Value);
                int kicker = groups.First(g => g.Count == 1).Value;
                return new HandRank(HandCategory.TwoPair, new[] { highPair, lowPair, kicker }, player);
            }

            if (groups[0].Count == 2)
            {
                int pair = groups[0].Value;
                var kickers = groups.Where(g => g.Count == 1).Select(g => g.Value).OrderByDescending(v => v);
                return new HandRank(HandCategory.Pair, (new[] { pair }).Concat(kickers), player);
            }

            // High card
            return new HandRank(HandCategory.HighCard, values, player);
        }

        private static (bool isStraight, int top) IsStraight(Card[] cards)
        {
            var vals = cards.Select(c => c.Value).Distinct().ToList();
            vals.Sort();

            // wheel check (A=14 treated as 1)
            if (vals.Contains(14))
                vals.Add(1);

            int run = 1;
            int bestTop = 0;
            for (int i = 1; i < vals.Count; i++)
            {
                if (vals[i] == vals[i-1] + 1)
                {
                    run++;
                }
                else if (vals[i] != vals[i-1])
                {
                    run = 1;
                }
                if (run >= 5)
                    bestTop = Math.Max(bestTop, vals[i]);
            }
            return (bestTop > 0, bestTop);
        }
    }
}
