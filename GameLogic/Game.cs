using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PokerServer.GameLogic.poker;

namespace PokerServer.GameLogic
{
    public class Game
    {
        public string Code { get; }
        private readonly List<Player> _players = new();
        private readonly object _lock = new();
        private bool started = false;
        private Round _round;

        public Game(string code)
        {
            Code = code;
        }

        public bool isEmpty()
        {
            return _players.Count == 0;
        }

        public async Task AddPlayerAsync(string id, string name, System.Net.WebSockets.WebSocket socket)
        {
            lock (_lock)
            {
                if (_players.Any(p => p.Id == id)) return;
                bool isHost = _players.Count == 0;
                _players.Add(new Player(id, name, socket, isHost));
            }

            await BroadcastStateAsync();
        }

        public void RemovePlayer(string id)
        {
            lock (_lock)
            {
                _players.RemoveAll(p => p.Id == id);
            }
        }

        public async Task StartGameAsync()
        {
            started = true;
            await BroadcastAsync(new { type = "gameStarted" });

            _round = new Round(_players);
            _round.startRound();
            await BroadcastAsync(new { type = "yourTurn", playerId = _round.getCurrentPlayerId() });
            await BroadcastAsync(new { type = "update", currentBet = _round.betSize});
            await BroadcastStateAsync();
        }

        public async Task PlayerActionAsync(string id, string action, int amount)
        {
            if (_round != null)
            {
                if (id == _round.getCurrentPlayerId())
                {
                    Console.WriteLine($"{id} performed {action} ({amount})");
                    if (_round.doAction(action, amount))
                    {
                        await BroadcastStateAsync();
                        if (await _round.endCycle(BroadcastStateAsyncShowCards, BroadcastAsync))
                        {
                            await Task.Delay(500);
                            await BroadcastAsync(new { type = "deal", board = _round.board.Select(c => c.ToString()).ToArray(), pot = _round.Pot });
                        }
                        await BroadcastAsync(new { type = "yourTurn", playerId = _round.getCurrentPlayerId() });
                        await BroadcastAsync(new { type = "update", currentBet = _round.betSize});
                        await BroadcastStateAsync();
                    }
                    
                }
                
            }
            
        }

        private async Task BroadcastStateAsync()
        {
            foreach (var player in _players)
            {

                var players = _players.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    money = p.Money,
                    betAmount = p.Bet,
                    isHost = p.IsHost,
                    hand = started && p.Id == player.Id ? p.hand : null
                }).ToArray();

                var payload = new { type = "playerList", players, youId = player.Id };
                await player.SendAsync(payload);
            }
        }
        
        private async Task BroadcastStateAsyncShowCards()
        {
            foreach (var player in _players)
            {
                
                var players = _players.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    money = p.Money,
                    betAmount = p.Bet,
                    isHost = p.IsHost,
                    hand = p.hand
                }).ToArray();

                var payload = new { type = "playerList", players, youId = player.Id };
                await player.SendAsync(payload);
            }
        }

        private async Task BroadcastAsync(object msg)
        {
            foreach (var p in _players)
                await p.SendAsync(msg);
        }
    }
}
