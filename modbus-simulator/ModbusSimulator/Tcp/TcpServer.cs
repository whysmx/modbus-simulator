using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using ModbusSimulator.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ModbusSimulator.Tcp;

public class TcpServer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<int, TcpListener> _listeners = new();
    private readonly ConcurrentDictionary<int, ConnectionInfo> _portToConnectionInfo = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public TcpServer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public async Task StartAsync(Dictionary<int, string> portToConnectionId)
    {
        if (portToConnectionId.Count == 0)
        {
            throw new ArgumentException("Port to connection ID dictionary cannot be empty", nameof(portToConnectionId));
        }
        
        // 使用 scoped service 获取协议信息
        using var scope = _serviceProvider.CreateScope();
        var connectionService = scope.ServiceProvider.GetRequiredService<ModbusSimulator.Services.IConnectionService>();
        
        // 获取每个连接的协议类型信息
        foreach (var kv in portToConnectionId)
        {
            var connectionTree = (await connectionService.GetConnectionsTreeAsync())
                .FirstOrDefault(c => c.Id == kv.Value);
            
            if (connectionTree != null)
            {
                _portToConnectionInfo[kv.Key] = new ConnectionInfo
                {
                    Id = kv.Value,
                    ProtocolType = connectionTree.ProtocolType
                };
            }
        }
        
        // Start all listeners but don't await them (they run continuously)
        foreach (var port in portToConnectionId.Keys)
        {
            _ = Task.Run(() => StartListenerAsync(port), _cancellationTokenSource.Token);
        }
        
        // Give listeners time to start
        await Task.Delay(100);
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
            ConnectionId = _portToConnectionInfo.TryGetValue(port, out var info) ? info.Id : null,
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
                
                // 为每个请求创建新的作用域以获取协议处理器
                using var scope = _serviceProvider.CreateScope();
                var protocolHandlerFactory = scope.ServiceProvider.GetRequiredService<IProtocolHandlerFactory>();
                
                // 根据连接的协议类型创建相应的协议处理器
                var protocolType = _portToConnectionInfo.TryGetValue(port, out var connectionInfo) 
                    ? connectionInfo.ProtocolType 
                    : ModbusProtocolType.ModbusRtuOverTcp; // 默认协议
                
                var protocolHandler = protocolHandlerFactory.CreateHandler(protocolType);
                var response = await protocolHandler.ProcessRequestAsync(request, context);
                
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
            _portToConnectionInfo.TryRemove(port, out _);
        }
    }
    
    public async Task StopAllAsync()
    {
        _cancellationTokenSource.Cancel();
        foreach (var listener in _listeners.Values) listener.Stop();
        _listeners.Clear();
        _portToConnectionInfo.Clear();
    }
    
    public object GetStatus()
    {
        var activeListeners = _listeners.Keys.Select(port => new 
        {
            Port = port,
            IsRunning = _listeners[port]?.Server?.IsBound ?? false,
            ConnectionId = _portToConnectionInfo.TryGetValue(port, out var info) ? info.Id : null,
            ProtocolType = _portToConnectionInfo.TryGetValue(port, out var info2) ? info2.ProtocolType.ToString() : "Unknown"
        }).ToArray();
        
        return new 
        {
            IsRunning = _listeners.Any(),
            ActivePorts = _listeners.Keys.ToArray(),
            TotalListeners = _listeners.Count,
            Listeners = activeListeners
        };
    }
}

public class ConnectionInfo
{
    public string Id { get; set; } = string.Empty;
    public ModbusProtocolType ProtocolType { get; set; }
}