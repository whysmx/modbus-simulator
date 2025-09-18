using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Services;

public class SlaveServiceTests
{
    private readonly Mock<ISlaveRepository> _mockSlaveRepository;
    private readonly Mock<IConnectionRepository> _mockConnectionRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly SlaveService _service;

    public SlaveServiceTests()
    {
        _mockSlaveRepository = new Mock<ISlaveRepository>();
        _mockConnectionRepository = new Mock<IConnectionRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _service = new SlaveService(_mockSlaveRepository.Object, _mockConnectionRepository.Object, _mockCacheService.Object);
    }

    #region CreateSlaveAsync Tests

    [Fact]
    public async Task CreateSlaveAsync_ValidRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        var expectedSlave = new Slave
        {
            Id = "generated-slave-id",
            Connid = connectionId,
            Name = "Test Slave",
            Slaveid = 1
        };
        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>())).ReturnsAsync(expectedSlave);

        // Act
        var result = await _service.CreateSlaveAsync(connectionId, request);

        // Assert
        Assert.Equal(expectedSlave, result);
        _mockSlaveRepository.Verify(r => r.CreateAsync(It.Is<Slave>(s =>
            s.Connid == connectionId && s.Name == "Test Slave" && s.Slaveid == 1)), Times.Once);
    }

    [Fact]
    public async Task CreateSlaveAsync_EmptyConnectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSlaveAsync("", request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Fact]
    public async Task CreateSlaveAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "non-existent-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(new List<ConnectionTree>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("连接不存在", exception.Message);
    }

    [Fact]
    public async Task CreateSlaveAsync_EmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("从机名称不能为空", exception.Message);
        Assert.Equal("request.Name", exception.ParamName);
    }

    [Fact]
    public async Task CreateSlaveAsync_NameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = new string('A', 101), Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("从机名称长度不能超过100个字符", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(248)]
    [InlineData(-1)]
    public async Task CreateSlaveAsync_InvalidSlaveId_ShouldThrowArgumentException(int invalidSlaveId)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = invalidSlaveId };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("从机地址必须在1-247之间", exception.Message);
        Assert.Equal("request.Slaveid", exception.ParamName);
    }

    [Fact]
    public async Task CreateSlaveAsync_NameTrimming_ShouldTrimWhitespace()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "  Test Slave  ", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ReturnsAsync((Slave s) => s);

        // Act
        await _service.CreateSlaveAsync(connectionId, request);

        // Assert
        _mockSlaveRepository.Verify(r => r.CreateAsync(It.Is<Slave>(s =>
            s.Name == "Test Slave")), Times.Once);
    }

    [Fact]
    public async Task CreateSlaveAsync_DuplicateSlaveId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Test Slave", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: slaves.slaveid", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("该连接下已存在相同地址的从机", exception.Message);
    }

    [Fact]
    public async Task CreateSlaveAsync_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var request = new CreateSlaveRequest { Name = "Duplicate Name", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree { Id = connectionId, Name = "Test Connection", Port = 502, Slaves = new List<Slave>() }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        _mockSlaveRepository.Setup(r => r.CreateAsync(It.IsAny<Slave>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: slaves.name", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateSlaveAsync(connectionId, request));
        Assert.Contains("该连接下已存在相同名称的从机", exception.Message);
    }

    #endregion

    #region UpdateSlaveAsync Tests

    [Fact]
    public async Task UpdateSlaveAsync_ValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new UpdateSlaveRequest { Name = "Updated Slave", Slaveid = 2 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = slaveId, Connid = connectionId, Name = "Original", Slaveid = 1 } }
            }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        var expectedSlave = new Slave
        {
            Id = slaveId,
            Connid = connectionId,
            Name = "Updated Slave",
            Slaveid = 2
        };
        _mockSlaveRepository.Setup(r => r.UpdateAsync(It.IsAny<Slave>())).ReturnsAsync(expectedSlave);

        // Act
        var result = await _service.UpdateSlaveAsync(connectionId, slaveId, request);

        // Assert
        Assert.Equal(expectedSlave, result);
        _mockSlaveRepository.Verify(r => r.UpdateAsync(It.Is<Slave>(s =>
            s.Id == slaveId && s.Connid == connectionId && s.Name == "Updated Slave" && s.Slaveid == 2)), Times.Once);
    }

    [Fact]
    public async Task UpdateSlaveAsync_EmptyConnectionId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new UpdateSlaveRequest { Name = "Test", Slaveid = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateSlaveAsync("", "slave-id", request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Fact]
    public async Task UpdateSlaveAsync_EmptySlaveId_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new UpdateSlaveRequest { Name = "Test", Slaveid = 1 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateSlaveAsync("connection-id", "", request));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Fact]
    public async Task UpdateSlaveAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "non-existent-connection";
        var slaveId = "slave-id";
        var request = new UpdateSlaveRequest { Name = "Test", Slaveid = 1 };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(new List<ConnectionTree>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateSlaveAsync(connectionId, slaveId, request));
        Assert.Contains("连接不存在", exception.Message);
    }

    [Fact]
    public async Task UpdateSlaveAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "non-existent-slave";
        var request = new UpdateSlaveRequest { Name = "Test", Slaveid = 1 };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = "different-slave-id", Name = "Other Slave", Slaveid = 2 } }
            }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateSlaveAsync(connectionId, slaveId, request));
        Assert.Contains("从机不存在", exception.Message);
    }

    #endregion

    #region DeleteSlaveAsync Tests

    [Fact]
    public async Task DeleteSlaveAsync_ValidIds_ShouldDeleteSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = slaveId, Name = "Test Slave", Slaveid = 1 } }
            }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        _mockSlaveRepository.Setup(r => r.DeleteAsync(slaveId)).Returns(Task.CompletedTask);

        // Act
        await _service.DeleteSlaveAsync(connectionId, slaveId);

        // Assert
        _mockSlaveRepository.Verify(r => r.DeleteAsync(slaveId), Times.Once);
    }

    [Fact]
    public async Task DeleteSlaveAsync_EmptyConnectionId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeleteSlaveAsync("", "slave-id"));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Fact]
    public async Task DeleteSlaveAsync_EmptySlaveId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeleteSlaveAsync("connection-id", ""));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Fact]
    public async Task DeleteSlaveAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "conn";
        var slaveId = "missing";

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = "other", Connid = connectionId, Name = "Other", Slaveid = 2 } }
            }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteSlaveAsync(connectionId, slaveId));
        Assert.Contains("从机不存在", ex.Message);
    }

    [Fact]
    public async Task DeleteSlaveAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "non-existent-connection";
        var slaveId = "slave-id";

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(new List<ConnectionTree>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteSlaveAsync(connectionId, slaveId));
        Assert.Contains("连接不存在", exception.Message);
    }

    #endregion
}
