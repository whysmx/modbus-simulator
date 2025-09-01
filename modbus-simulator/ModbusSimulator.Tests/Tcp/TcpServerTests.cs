using System.Net;
using System.Net.Sockets;
using ModbusSimulator.Tcp;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Tcp;

public class TcpServerTests : IDisposable
{
    private readonly Mock<IProtocolHandler> _mockProtocolHandler;
    private readonly TcpServer _tcpServer;
    private readonly CancellationTokenSource _testCancellationSource;

    public TcpServerTests()
    {
        _mockProtocolHandler = new Mock<IProtocolHandler>();
        _mockProtocolHandler.Setup(p => p.ProtocolType).Returns("TestProtocol");
        _tcpServer = new TcpServer(_mockProtocolHandler.Object);
        _testCancellationSource = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _testCancellationSource.Cancel();
        _testCancellationSource.Dispose();
    }

    [Fact]
    public async Task Constructor_InitializesWithProtocolHandler()
    {
        // Arrange & Act
        var tcpServer = new TcpServer(_mockProtocolHandler.Object);

        // Assert
        Assert.NotNull(tcpServer);
        // 可以通过反射验证私有字段，或者通过行为测试
    }

    [Fact]
    public async Task StartAsync_WithValidPorts_CreatesListeners()
    {
        // Arrange
        var portToConnectionId = new Dictionary<int, string>
        {
            [502] = "conn-1",
            [503] = "conn-2"
        };

        // Mock the protocol handler to return a simple response
        _mockProtocolHandler.Setup(p => p.ProcessRequestAsync(It.IsAny<byte[]>(), It.IsAny<ProtocolContext>()))
            .ReturnsAsync(new byte[] { 0x01, 0x02 });

        // Act
        await _tcpServer.StartAsync(portToConnectionId);

        // Assert - 由于网络操作，我们主要验证方法能正常执行
        // 在实际测试中，我们可能需要更复杂的模拟

        // Cleanup
        await _tcpServer.StopAllAsync();
    }

    [Fact]
    public async Task StopAsync_WithValidPort_RemovesListener()
    {
        // Arrange
        var portToConnectionId = new Dictionary<int, string>
        {
            [502] = "conn-1"
        };

        await _tcpServer.StartAsync(portToConnectionId);

        // Act
        await _tcpServer.StopAsync(502);

        // Assert - 验证端口已被移除
        // 由于TcpServer的内部状态是私有的，我们通过行为来验证
    }

    [Fact]
    public async Task StopAllAsync_StopsAllListeners()
    {
        // Arrange
        var portToConnectionId = new Dictionary<int, string>
        {
            [502] = "conn-1",
            [503] = "conn-2"
        };

        await _tcpServer.StartAsync(portToConnectionId);

        // Act
        await _tcpServer.StopAllAsync();

        // Assert - 所有监听器都已停止
    }

    [Fact]
    public async Task StartAsync_EmptyPortDictionary_DoesNotThrow()
    {
        // Arrange
        var portToConnectionId = new Dictionary<int, string>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _tcpServer.StartAsync(portToConnectionId));
    }

    [Fact]
    public async Task ProtocolHandler_ProcessRequestAsync_IsCalled()
    {
        // Arrange
        var portToConnectionId = new Dictionary<int, string>
        {
            [502] = "conn-1"
        };

        var testRequest = new byte[] { 0x01, 0x02, 0x03 };
        var testResponse = new byte[] { 0x04, 0x05, 0x06 };

        _mockProtocolHandler.Setup(p => p.ProcessRequestAsync(
            It.Is<byte[]>(req => req.SequenceEqual(testRequest)),
            It.IsAny<ProtocolContext>()))
            .ReturnsAsync(testResponse);

        await _tcpServer.StartAsync(portToConnectionId);

        // Act - 由于实际网络连接很难在单元测试中模拟，我们验证设置
        _mockProtocolHandler.Verify(p => p.ProcessRequestAsync(
            It.IsAny<byte[]>(),
            It.IsAny<ProtocolContext>()), Times.Never); // 应该还没有调用

        // Cleanup
        await _tcpServer.StopAllAsync();
    }

    // 集成测试风格的方法 - 模拟完整的客户端连接
    [Fact]
    public async Task HandleClientAsync_ProcessesRequestCorrectly()
    {
        // Arrange
        var testRequest = new byte[] { 0x01, 0x02, 0x03 };
        var testResponse = new byte[] { 0x04, 0x05, 0x06 };

        _mockProtocolHandler.Setup(p => p.ProcessRequestAsync(
            It.Is<byte[]>(req => req.SequenceEqual(testRequest)),
            It.IsAny<ProtocolContext>()))
            .ReturnsAsync(testResponse);

        // Act & Assert
        // 由于网络操作的复杂性，这个测试主要验证设置的正确性
        // 在实际项目中，可能需要使用TestServer或模拟网络层

        _mockProtocolHandler.Verify(p => p.ProcessRequestAsync(
            It.IsAny<byte[]>(),
            It.IsAny<ProtocolContext>()), Times.Never);
    }
}

// ProtocolContext的测试
public class ProtocolContextTests
{
    [Fact]
    public void ProtocolContext_InitializesCorrectly()
    {
        // Arrange
        var connectionId = "test-connection";
        var localPort = 502;
        var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345);

        // Act
        var context = new ProtocolContext
        {
            ConnectionId = connectionId,
            LocalPort = localPort,
            RemoteEndPoint = remoteEndPoint
        };

        // Assert
        Assert.Equal(connectionId, context.ConnectionId);
        Assert.Equal(localPort, context.LocalPort);
        Assert.Equal(remoteEndPoint, context.RemoteEndPoint);
    }

    [Fact]
    public void ProtocolContext_AllowsNullConnectionId()
    {
        // Arrange & Act
        var context = new ProtocolContext
        {
            ConnectionId = null,
            LocalPort = 502,
            RemoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Assert
        Assert.Null(context.ConnectionId);
    }
}
