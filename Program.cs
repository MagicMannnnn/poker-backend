using System.Net.WebSockets;
using PokerServer.WebSocket;
using PokerServer.GameLogic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseWebSockets();

var handler = new PokerWebSocketHandler(new GameManager());

app.Map("/ws", handler.HandleWebSocketAsync);

builder.WebHost.UseShutdownTimeout(TimeSpan.FromSeconds(2));
app.Run("http://localhost:5000");
