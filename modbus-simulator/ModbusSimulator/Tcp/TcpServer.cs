using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace ModbusSimulator.Tcp;

public class TcpServer
{
    private readonly IProtocolHandler _protocolHandler;
    private readonly ConcurrentDictionary<int, TcpListener> _listeners = new();
    private readonly ConcurrentDictionary<int, string> _portToConnectionId = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public TcpServer(IProtocolHandler protocolHandler)
    {
        _protocolHandler = protocolHandler;
    }
    
    public async Task StartAsync(Dictionary<int, string> portToConnectionId)
    {
        foreach (var kv in portToConnectionId)
        {
            _portToConnectionId[kv.Key] = kv.Value;
        }
        var tasks = portToConnectionId.Keys.Select(StartListenerAsync);
        await Task.WhenAll(tasks);
    }
    
    private async Task StartListenerAsync(int port)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        _listeners[port] = listener;
        listener.Start();
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => HandleClientAsync(port, client), _cancellationTokenSource.Token);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }
    
    private async Task HandleClientAsync(int port, TcpClient client)
    {
        using var networkStream = client.GetStream();
        var buffer = new byte[1024];
        var context = new ProtocolContext
        {
            LocalPort = port,
            ConnectionId = _portToConnectionId.TryGetValue(port, out var id) ? id : null,
            RemoteEndPoint = client.Client.RemoteEndPoint
        };
        
        try
        {
            while (client.Connected && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                
                var request = new byte[bytesRead];
                Array.Copy(buffer, 0, request, 0, bytesRead);
                
                var response = await _protocolHandler.ProcessRequestAsync(request, context);
                
                await networkStream.WriteAsync(response, 0, response.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"客户端连接处理错误: {ex.Message}");
        }
        finally
        {
            client?.Close();
        }
    }
    
    public async Task StopAsync(int port)
    {
        if (_listeners.TryRemove(port, out var listener))
        {
            listener.Stop();
            _portToConnectionId.TryRemove(port, out _);
        }
    }
    
    public async Task StopAllAsync()
    {
        _cancellationTokenSource.Cancel();
        foreach (var listener in _listeners.Values) listener.Stop();
        _listeners.Clear();
        _portToConnectionId.Clear();
    }
}