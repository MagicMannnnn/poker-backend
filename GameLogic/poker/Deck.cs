using System;
using System.Collections.Generic;

namespace PokerServer.GameLogic.poker
{
    public class Deck
    {
        private List<Card> cards;

        public Deck()
        {
            cards = new List<Card>();
            Reset();
        }

        public void Reset()
        {
            cards.Clear();

            string[] suits = { "Clubs", "Hearts", "Spades", "Diamonds" };
            string[] values = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };

            foreach (var suit in suits)
            {
                foreach (var value in values)
                {
                    cards.Add(new Card(suit, value));
                }
            }

            Shuffle();
        }

        public void Shuffle()
        {
            var rng = new Random();
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (cards[k], cards[n]) = (cards[n], cards[k]); // tuple swap
            }
        }

        public Card Pop()
        {
            if (cards.Count == 0)
            {
                Reset();
            }

            var card = cards[^1]; // last card
            cards.RemoveAt(cards.Count - 1);
            return card;
        }

        public override string ToString()
        {
            return string.Join(", ", cards);
        }
    }
}
