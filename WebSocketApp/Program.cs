﻿using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace WebSocketApp;

internal class Program
{
    static readonly CancellationTokenSource tokenSource = new();
    static async Task Main(string[] args)
    {
        Console.WriteLine("WS Server configure");

        using HttpListener listener = new HttpListener();

        string serverUrl1 = "http://localhost:8080/ws/";
        string serverUrl2 = "http://localhost:8080/";

        listener.Prefixes.Add(serverUrl1);
        listener.Prefixes.Add(serverUrl2);
        listener.Start();

        _ = Task.Run(() =>
        {
            Console.WriteLine("Type \"stop\" to interrupt the job :)");
            while (true)
            {
                var input = Console.ReadLine();
                if (input == "stop")
                {
                    tokenSource.Cancel();
                    listener.Abort();
                    Console.WriteLine("WS Server stopped...");
                    break;
                }
            }
        });

        Console.WriteLine("WS Server started, waiting connection...");
        try
        {
            while (tokenSource.IsCancellationRequested == false)
            {
                var context = await listener.GetContextAsync();
                _ = Task.Run(() => HandleConnectionAsync(context));
            }
        }
        catch (HttpListenerException)
        {
        }
    }

    static async Task HandleConnectionAsync(HttpListenerContext context)
    {
        if(context.Request.IsWebSocketRequest == false)
        {
            context.Response.StatusCode = 200;
            context.Response.Close(Encoding.UTF8.GetBytes("Only WS connection allowed"), false);
            return;
        }

        var wsContext = await context.AcceptWebSocketAsync(null);
        var wsSocket = wsContext.WebSocket;

        Console.WriteLine($"WS {wsSocket} connection established");
        var buffer = WebSocket.CreateClientBuffer(4096, 4096);
        try
        {
            var result = await wsSocket.ReceiveAsync(buffer, tokenSource.Token);
            while (result.CloseStatus.HasValue == false || result.MessageType != WebSocketMessageType.Close)
            {
                var message = Encoding.UTF8.GetString(buffer);
                Console.WriteLine($"Message {message} received");

                var sendMessage = Encoding.UTF8.GetBytes($"WS Server echo: {message}");
                await wsSocket.SendAsync(sendMessage, WebSocketMessageType.Text, true, tokenSource.Token);

                result = await wsSocket.ReceiveAsync(buffer, tokenSource.Token);
            }

            if (result.CloseStatus is not null)
            {
                Console.WriteLine($"WS connection closing ....");
                await wsSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, tokenSource.Token);

                Console.WriteLine($"WS connection closed");
            }
        }
        catch(OperationCanceledException)
        { 
        }
    }
}