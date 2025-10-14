using System.Net.WebSockets;

namespace PokerServer.GameLogic
{
    public class GameManager
    {
        private readonly object _lock = new();
        private readonly Dictionary<string, Game> _games = new();

        public async Task AddPlayerAsync(string code, string id, string name, System.Net.WebSockets.WebSocket socket)
        {
            Game game;
            lock (_lock)
            {
                if (!_games.TryGetValue(code, out game!))
                {
                    game = new Game(code);
                    _games[code] = game;
                }
            }
            await game.AddPlayerAsync(id, name, socket);
        }

        public async Task RemovePlayerAsync(string code, string id)
        {
            lock (_lock)
            {
                if (_games.TryGetValue(code, out var g))
                {
                    g.RemovePlayer(id);
                    if (g.isEmpty())
                    {
                        _games.Remove(code);
                    }
                }
                    

            }
        }

        public async Task StartGameAsync(string code)
        {
            if (_games.TryGetValue(code, out var g))
                await g.StartGameAsync();
        }

        public async Task PlayerActionAsync(string code, string id, string action, int amount)
        {
            if (_games.TryGetValue(code, out var g))
                await g.PlayerActionAsync(id, action, amount);
        }
    }
}
