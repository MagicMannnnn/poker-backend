using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PokerServer.GameLogic.poker;

namespace PokerServer.GameLogic
{
    public class Player
    {
        public string Id { get; }
        public string Name { get; }
        public int Money { get; set; } = 1000;
        public int Bet { get; set; }
        public bool IsHost { get; }

        public string[] hand { get; }
        public Card[] realhand { get; }
        public bool isPlaying { get; set; } = false;

        private readonly System.Net.WebSockets.WebSocket _socket;

        public Player(string id, string name, System.Net.WebSockets.WebSocket socket, bool isHost)

        {
            Id = id;
            Name = name;
            _socket = socket;
            IsHost = isHost;
            realhand = new Card[2];
            hand = new string[2];
        }

        public void setHand(Card c1, Card c2)
        {
            hand[0] = c1.ToString();
            hand[1] = c2.ToString();
            realhand[0] = c1;
            realhand[1] = c2;
        }

        public async Task SendAsync(object obj)
        {
            if (_socket.State != WebSocketState.Open) return;
            try
            {
                var json = JsonSerializer.Serialize(obj);
                var buffer = Encoding.UTF8.GetBytes(json);
                await _socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch { }
        }
    }
}
