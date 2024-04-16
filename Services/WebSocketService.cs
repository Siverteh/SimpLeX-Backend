using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using SimpLeX_Backend.Data;
using SimpLeX_Backend.Models;

public class WebSocketService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private ConcurrentDictionary<string, Dictionary<WebSocket, string>> _socketsByProject =
        new ConcurrentDictionary<string, Dictionary<WebSocket, string>>();

    public void AddSocketToProject(string projectId, WebSocket socket, string userName)
    {
        var sockets = _socketsByProject.GetOrAdd(projectId, _ => new Dictionary<WebSocket, string>());
        lock (sockets)
        {
            sockets[socket] = userName;
            BroadcastCollaborators(projectId); // Use Task.Run to avoid deadlock in lock
        }
    }

    public async Task RemoveSocketFromProject(string projectId, WebSocket socket)
    {
        if (_socketsByProject.TryGetValue(projectId, out var sockets))
        {
            bool shouldBroadcast = false;
            lock (sockets)
            {
                if (sockets.Remove(socket) && sockets.Count > 0)
                {
                    shouldBroadcast = true;
                }
            }

            if (shouldBroadcast)
            {
                BroadcastCollaborators(projectId);
            }
        }
    }


    public async Task HandleWebSocketAsync(string projectId, WebSocket webSocket)
    {
        var buffer = new byte[1024 * 4];
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
                        Console.WriteLine("case newChat???");
                        await SaveChatToDb(projectId, message.Data);
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

        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        await RemoveSocketFromProject(projectId, webSocket);
    }

    private async Task SaveChatToDb(string projectId, dynamic chatData)
    {
        // Validation for projectId
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new ArgumentException("Project ID cannot be null or empty.", nameof(projectId));
        }

        // Validation for userId
        if (string.IsNullOrWhiteSpace(chatData.userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(chatData.userId));
        }

        // Validation for message content
        if (string.IsNullOrWhiteSpace(chatData.content))
        {
            throw new ArgumentException("Message content cannot be null or empty.", nameof(chatData.content));
        }

        // Validation for timestamp
        if (chatData.timestamp == null || !DateTime.TryParse(chatData.timestamp, out DateTime parsedTimestamp))
        {
            throw new ArgumentException("Timestamp is either null or not in a valid DateTime format.",
                nameof(chatData.timestamp));
        }

        Console.WriteLine("hello guys");
        // Prepare model data.
        var chat = new ChatMessage
        {
            ProjectId = projectId,
            UserId = chatData.userId,
            Message = chatData.content,
            Timestamp = chatData.timestamp,
        };

        // Add chat to database.
        _context.ChatMessages.Add(chat);

        // Save changes to the database.
        await _context.SaveChangesAsync();
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