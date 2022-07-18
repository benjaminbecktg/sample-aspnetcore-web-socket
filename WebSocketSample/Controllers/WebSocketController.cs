using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;

namespace WebSocketSample.Controllers
{
    [ApiController]
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        static IDictionary<Guid, WebSocket> wsList = new Dictionary<Guid, WebSocket>();

        public WebSocketController()
        {
            
        }

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await Echo(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private static async Task Echo(WebSocket webSocket)
        {
            var socketID = Guid.NewGuid();
            Console.WriteLine($"{socketID} - Open connection...");

            wsList.Add(socketID, webSocket);

            var buffer = new byte[1024 * 4];
            var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                    receiveResult.MessageType,
                    receiveResult.EndOfMessage,
                    CancellationToken.None);

                foreach (var key in wsList.Keys)
                {
                    if (!key.Equals(socketID))
                    {
                        WebSocket ws = wsList[key];
                        await ws.SendAsync(
                            new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                            receiveResult.MessageType,
                            receiveResult.EndOfMessage,
                            CancellationToken.None);
                    }
                }

                var messageFromClient = System.Text.UTF8Encoding.ASCII.GetString(buffer);

                Console.WriteLine($"{socketID} - Ping from client with message: {messageFromClient}");
                receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            await webSocket.CloseAsync(
                receiveResult.CloseStatus.Value,
                receiveResult.CloseStatusDescription,
                CancellationToken.None);

            Console.WriteLine($"{socketID} - Close connection...");
            wsList.Remove(socketID);
        }
    }
}