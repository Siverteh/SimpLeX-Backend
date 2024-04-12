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
    private ConcurrentDictionary<string, Dictionary<WebSocket, string>> _socketsByProject = new ConcurrentDictionary<string, Dictionary<WebSocket, string>>();

    public void AddSocketToProject(string projectId, WebSocket socket, string userName)
    {
        var sockets = _socketsByProject.GetOrAdd(projectId, _ => new Dictionary<WebSocket, string>());
        Console.WriteLine("Issue here 6");
        lock (sockets)
        {
            Console.WriteLine("Issue here 7");
            sockets[socket] = userName;
            BroadcastCollaborators(projectId);  // Use Task.Run to avoid deadlock in lock
        }
    }

    public async Task RemoveSocketFromProject(string projectId, WebSocket socket)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            Console.WriteLine("Issue here 8");
            bool shouldBroadcast = false;
            lock (sockets)
            {
                Console.WriteLine("Issue here 9");
                if (sockets.Remove(socket) && sockets.Count > 0)
                {
                    Console.WriteLine("Issue here 10");
                    shouldBroadcast = true;
                }
            }
            if (shouldBroadcast)
            {
                Console.WriteLine("Issue here 11");
                BroadcastCollaborators(projectId);
            }
        }
    }


    public async Task HandleWebSocketAsync(string projectId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        Console.WriteLine("Issue here 12");
        WebSocketReceiveResult result =
            await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        Console.WriteLine("Issue here 13");
        
        while (!result.CloseStatus.HasValue)
        {
            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                dynamic message = JsonConvert.DeserializeObject(messageJson);
                string action = message.Action;
                Console.WriteLine("Issue here 14");
                switch (action)
                {
                    case "cursorMove":
                        await BroadcastCursorMove(projectId, webSocket, message.Data);
                        break;
                    case "blocklyUpdateImportant":
                        await BroadcastBlocklyUpdateImportant(projectId, webSocket, message.Data);
                        break;
                    case "blocklyUpdate":
                        await BroadcastBlocklyUpdate(projectId, webSocket, message.Data);
                        break;
                    case "newChat":
                        //saveChatToDB()
                        BroadcastNewChat(projectId, webSocket, message.Data);
                        break;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        Console.WriteLine("Issue here 15");
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        Console.WriteLine("Issue here 16");
        await RemoveSocketFromProject(projectId, webSocket);
        Console.WriteLine("Issue here 17");
    }

    private async Task BroadcastCollaborators(string projectId)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var collaborators = sockets.Values.Select(userName => new { userName }).ToList();
            var message = JsonConvert.SerializeObject(new { Action = "updateCollaborators", Data = collaborators });
            Console.WriteLine("Sending message to clients: " + message);

            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = sockets.Keys.Where(socket => socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, segment, projectId)); // Notice the projectId is now passed
            await Task.WhenAll(tasks);
        }
    }

    

    private async Task BroadcastCursorMove(string projectId, WebSocket sender, dynamic cursorPosition)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var data = new { Action = "cursorMove", Data = cursorPosition };
            var message = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = sockets.Keys.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, segment, projectId));
            await Task.WhenAll(tasks);
        }
    }

    private async Task BroadcastBlocklyUpdateImportant(string projectId, WebSocket sender, dynamic blocklyData)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var data = new { Action = "blocklyUpdateImportant", Data = blocklyData };
            var message = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = sockets.Keys.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, segment, projectId));
            await Task.WhenAll(tasks);
        }
    }


    private async Task BroadcastBlocklyUpdate(string projectId, WebSocket sender, dynamic blocklyData)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var data = new { Action = "blocklyUpdate", Data = blocklyData };
            var message = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = sockets.Keys.Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, segment, projectId));
            await Task.WhenAll(tasks);
        }
    }


    private async Task BroadcastNewChat(string projectId, WebSocket sender, dynamic chatData)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            var data = new { Action = "newChat", Data = chatData };
            var message = JsonConvert.SerializeObject(data);
            var buffer = Encoding.UTF8.GetBytes(message);
            var segment = new ArraySegment<byte>(buffer);

            var tasks = sockets.Keys
                .Where(socket => socket != sender && socket.State == WebSocketState.Open)
                .Select(socket => SafeSendAsync(socket, segment, projectId));
            await Task.WhenAll(tasks);
        }
    }

    
    private async Task SafeSendAsync(WebSocket socket, ArraySegment<byte> data, string projectId)
    {
        if (socket.State == WebSocketState.Open)
        {
            try
            {
                await socket.SendAsync(data, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send WebSocket message: {ex.Message}");
                // Call to remove the socket properly if it's no longer valid
                await RemoveSocketFromProject(projectId, socket);
            }
        }
        else
        {
            Console.WriteLine("WebSocket is not in an open state.");
            // Remove the socket as it's not open
            await RemoveSocketFromProject(projectId, socket);
        }
    }
}
