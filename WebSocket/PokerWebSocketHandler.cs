using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PokerServer.GameLogic;

namespace PokerServer.WebSocket
{
    public class PokerWebSocketHandler
    {
        private readonly GameManager _manager;

        public PokerWebSocketHandler(GameManager manager)
        {
            _manager = manager;
        }

        public async Task HandleWebSocketAsync(HttpContext ctx)
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            var socket = await ctx.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("Client connected!");

            var buffer = new byte[8192];
            string? gameCode = null;
            string playerId = Guid.NewGuid().ToString("n")[..8];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    if (result.Count == 0) continue;

                    var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var root = JsonDocument.Parse(msg).RootElement;
                    var type = root.GetProperty("type").GetString();

                    switch (type)
                    {
                        case "host":
                        case "join":
                            gameCode = root.GetProperty("gameCode").GetString() ?? "ABCD";
                            var username = root.GetProperty("username").GetString() ?? "Player";
                            await _manager.AddPlayerAsync(gameCode, playerId, username, socket);
                            break;

                        case "start":
                            if (gameCode != null)
                                await _manager.StartGameAsync(gameCode);
                            break;

                        case "action":
                            if (gameCode != null)
                            {
                                var action = root.GetProperty("action").GetString() ?? "check";
                                var amount = root.TryGetProperty("amount", out var amt) ? amt.GetInt32() : 0;
                                await _manager.PlayerActionAsync(gameCode, playerId, action, amount);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? WS error: {ex.Message}");
            }
            finally
            {
                if (gameCode != null)
                    await _manager.RemovePlayerAsync(gameCode, playerId);

                try { await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); }
                catch { }
                Console.WriteLine("Socket closed");
            }
        }
    }
}
