namespace PokerServer.GameLogic.poker
{
    

    public class Card
    {
        public string Suit { get; }
        public string ValueStr { get; }
        public int Value { get; }

        public Card(string suit, string value)
        {
            Suit = suit.ToLower();
            ValueStr = value;

            if (int.TryParse(value, out int numericValue))
            {
                Value = numericValue;
            }
            else
            {
                switch (value.ToLower())
                {
                    case "jack":
                        Value = 11;
                        break;
                    case "queen":
                        Value = 12;
                        break;
                    case "king":
                        Value = 13;
                        break;
                    case "ace":
                        Value = 14;
                        break;
                    default:
                        Value = 0; // fallback for invalid values
                        break;
                }

            }
        }

        public override string ToString()
        {
            return $"{ValueStr.ToLower()}_of_{Suit.ToLower()}";
        }
    }
}
