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
    private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);  // Create a semaphore for locking

    public async Task AddSocketToProject(string projectId, WebSocket socket, string userName)
    {
        await semaphore.WaitAsync();  // Asynchronously wait to enter the semaphore
        try
        {
            var sockets = _socketsByProject.GetOrAdd(projectId, _ => new Dictionary<WebSocket, string>());
            sockets[socket] = userName;
            Console.WriteLine($"Added {userName} to project {projectId}.");
            await BroadcastCollaborators(projectId);  // Await the broadcast within the semaphore
        }
        finally
        {
            semaphore.Release();  // Release the semaphore
        }
    }

    public async Task RemoveSocketFromProject(string projectId, WebSocket socket)
    {
        await semaphore.WaitAsync();
        try
        {
            if (_socketsByProject.TryGetValue(projectId, out var sockets))
            {
                sockets.Remove(socket);
                if (sockets.Count == 0)
                {
                    _socketsByProject.TryRemove(projectId, out _);
                }
                else
                {
                    await BroadcastCollaborators(projectId);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }


    public async Task HandleWebSocketAsync(string projectId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            try
            {
                dynamic message = JsonConvert.DeserializeObject(messageJson);
                string action = message.Action;
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
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
            }

            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        await RemoveSocketFromProject(projectId, webSocket);
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
