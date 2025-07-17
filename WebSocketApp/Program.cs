using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketApp;

internal class Program
{
    static readonly CancellationTokenSource tokenSource = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("WS Server configure");
        
        string serverUrl = "http://localhost:8080/ws/";
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(serverUrl);
        listener.Start();

        Console.WriteLine("WS Server started, waiting connection...");

        while (tokenSource.IsCancellationRequested == false) 
        {
            var context = await listener.GetContextAsync();
            _ = Task.Factory.StartNew(() => HandleConnectionAsync(context));
        }
    }

    static async Task HandleConnectionAsync(HttpListenerContext context)
    {
        if(context.Request.IsWebSocketRequest == false)
        {
            await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Only WS connection allowed"));
            context.Response.StatusCode = 200;
            context.Response.Close();
        }

        var wsContext = await context.AcceptWebSocketAsync(null);
        var wsSocket = wsContext.WebSocket;

        Console.WriteLine($"WS {wsSocket} connection established");
        var buffer = WebSocket.CreateClientBuffer(4096, 4096);

        var result = await wsSocket.ReceiveAsync(buffer, tokenSource.Token);
        while (result.CloseStatus.HasValue == false)
        {
            var message = Encoding.UTF8.GetString(buffer);
            Console.WriteLine($"Message {message} received");

            byte[] sendMessage = Encoding.UTF8.GetBytes($"Server echo: {message}");
            await wsSocket.SendAsync(new ArraySegment<byte>(sendMessage), WebSocketMessageType.Text, true, tokenSource.Token);

            result = await wsSocket.ReceiveAsync(buffer, tokenSource.Token);
        }

        if (result.CloseStatus is not null)
        {
            Console.WriteLine($"WS connection closing ....");
            await wsSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, tokenSource.Token);

            Console.WriteLine($"WS connection closed");
        }
    }
}