using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "Poker server is running.");

// Minimal WebSocket echo endpoint to prove things work.
// Swap this for your PokerWebSocketHandler wiring when ready.
app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[4 * 1024];

    var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
    while (!result.CloseStatus.HasValue)
    {
        await socket.SendAsync(buffer.AsMemory(0, result.Count), result.MessageType, result.EndOfMessage, context.RequestAborted);
        result = await socket.ReceiveAsync(buffer, context.RequestAborted);
    }

    await socket.CloseAsync(result.CloseStatus!.Value, result.CloseStatusDescription, context.RequestAborted);
});

await app.RunAsync();
