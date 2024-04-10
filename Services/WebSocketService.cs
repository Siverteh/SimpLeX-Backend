using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class WebSocketService
{
    private ConcurrentDictionary<string, HashSet<WebSocket>> _socketsByProject = new ConcurrentDictionary<string, HashSet<WebSocket>>();

    public void AddSocketToProject(string projectId, WebSocket socket)
    {
        var sockets = _socketsByProject.GetOrAdd(projectId, _ => new HashSet<WebSocket>());
        lock (sockets)
        {
            sockets.Add(socket);
        }
    }

    public void RemoveSocketFromProject(string projectId, WebSocket socket)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            lock (sockets)
            {
                sockets.Remove(socket);
                if (sockets.Count == 0)
                {
                    _socketsByProject.TryRemove(projectId, out _);
                }
            }
        }
    }

    public async Task HandleWebSocketAsync(string projectId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message: {messageJson}");

            try
            {
                dynamic message = JsonConvert.DeserializeObject(messageJson);
                string action = message.Action;
                switch (action)
                {
                    case "cursorMove":
                        BroadcastCursorMove(projectId, webSocket, message.Data);
                        break;
                    case "blocklyUpdateImportant":
                        BroadcastBlocklyUpdateImportant(projectId, webSocket, message.Data);
                        break;
                    case "blocklyUpdate":
                        BroadcastBlocklyUpdate(projectId, webSocket, message.Data);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        RemoveSocketFromProject(projectId, webSocket);
    }

    private async Task BroadcastCursorMove(string projectId, WebSocket sender, dynamic cursorPosition)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var tasks = sockets.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, new { Action = "cursorMove", Data = cursorPosition }));
            await Task.WhenAll(tasks);
        }
    }
    
    private async Task BroadcastBlocklyUpdateImportant(string projectId, WebSocket sender, dynamic blocklyData)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var tasks = sockets.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, new { Action = "blocklyUpdateImportant", Data = blocklyData }));
            await Task.WhenAll(tasks);
        }
    }

    private async Task BroadcastBlocklyUpdate(string projectId, WebSocket sender, dynamic blocklyData)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var tasks = sockets.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, new { Action = "blocklyUpdate", Data = blocklyData }));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SafeSendAsync(WebSocket socket, object data)
    {
        var message = JsonConvert.SerializeObject(data);
        var buffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(buffer);

        try
        {
            await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send WebSocket message: {ex.Message}");
            // Consider handling the failure, e.g., removing the faulty socket.
        }
    }

}
