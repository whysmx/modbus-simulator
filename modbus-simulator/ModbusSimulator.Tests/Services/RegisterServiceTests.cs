using Microsoft.Extensions.Caching.Memory;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Services;

public class RegisterServiceTests : IDisposable
{
    private readonly Mock<IRegisterRepository> _mockRegisterRepository;
    private readonly Mock<IConnectionRepository> _mockConnectionRepository;
    private readonly Mock<ISlaveRepository> _mockSlaveRepository;
    private readonly IMemoryCache _cache;
    private readonly RegisterService _service;

    public RegisterServiceTests()
    {
        _mockRegisterRepository = new Mock<IRegisterRepository>();
        _mockConnectionRepository = new Mock<IConnectionRepository>();
        _mockSlaveRepository = new Mock<ISlaveRepository>();
        
        // 使用真实的 MemoryCache 而不是 Mock
        _cache = new MemoryCache(new MemoryCacheOptions());
        
        _service = new RegisterService(
            _mockRegisterRepository.Object,
            _mockConnectionRepository.Object,
            _mockSlaveRepository.Object,
            _cache);
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }

    #region GetRegistersBySlaveIdAsync Tests

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_ValidSlaveId_ShouldReturnRegisters()
    {
        // Arrange
        var port = 502;
        var slaveAddress = "1"; // 从机地址字符串
        var slaveUuid = "test-slave-uuid"; // 从机UUID
        var expectedRegisters = new List<Register>
        {
            new Register { Id = "reg1", Slaveid = slaveUuid, Startaddr = 40001, Hexdata = "ABCD" },
            new Register { Id = "reg2", Slaveid = slaveUuid, Startaddr = 40002, Hexdata = "1234" }
        };

        // Mock connections tree with a connection and slave
        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = "conn1",
                Port = port,
                Slaves = new List<Slave>
                {
                    new Slave { Id = slaveUuid, Slaveid = 1, Name = "Test Slave" }
                }
            }
        };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);
        _mockRegisterRepository.Setup(r => r.GetBySlaveIdAsync(slaveUuid)).ReturnsAsync(expectedRegisters);

        // Act
        var result = await _service.GetRegistersBySlaveIdAsync(port, slaveAddress);

        // Assert
        Assert.Equal(expectedRegisters, result);
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid), Times.Once);
        
        // Test cache by calling again - should not hit repository twice
        var cachedResult = await _service.GetRegistersBySlaveIdAsync(port, slaveAddress);
        Assert.Equal(expectedRegisters, cachedResult);
        
        // Repository should still only be called once (from cache on second call)
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid), Times.Once);
    }

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_EmptySlaveId_ShouldThrowArgumentException()
    {
        // Arrange
        var port = 502;
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetRegistersBySlaveIdAsync(port, ""));
        Assert.Contains("从机地址不能为空", exception.Message);
        Assert.Equal("slaveAddress", exception.ParamName);
    }

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_DifferentPorts_ShouldHaveSeparateCaches()
    {
        // Arrange
        var port1 = 502;
        var port2 = 503;
        var slaveAddress = "1"; // 从机地址
        var slaveUuid1 = "test-slave-uuid-1";
        var slaveUuid2 = "test-slave-uuid-2";
        
        var registers1 = new List<Register>
        {
            new Register { Id = "reg1", Slaveid = slaveUuid1, Startaddr = 40001, Hexdata = "ABCD" }
        };
        
        var registers2 = new List<Register>
        {
            new Register { Id = "reg2", Slaveid = slaveUuid2, Startaddr = 40002, Hexdata = "1234" }
        };

        // Mock两个不同的连接，每个连接有不同的从机
        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = "conn1",
                Port = port1,
                Slaves = new List<Slave>
                {
                    new Slave { Id = slaveUuid1, Slaveid = 1, Name = "Test Slave 1" }
                }
            },
            new ConnectionTree
            {
                Id = "conn2", 
                Port = port2,
                Slaves = new List<Slave>
                {
                    new Slave { Id = slaveUuid2, Slaveid = 1, Name = "Test Slave 2" }
                }
            }
        };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);
        _mockRegisterRepository.Setup(r => r.GetBySlaveIdAsync(slaveUuid1)).ReturnsAsync(registers1);
        _mockRegisterRepository.Setup(r => r.GetBySlaveIdAsync(slaveUuid2)).ReturnsAsync(registers2);

        // Act
        var result1 = await _service.GetRegistersBySlaveIdAsync(port1, slaveAddress);
        var result2 = await _service.GetRegistersBySlaveIdAsync(port2, slaveAddress);

        // Assert
        Assert.Equal(registers1, result1);
        Assert.Equal(registers2, result2);
        
        // Both ports should trigger separate repository calls
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid1), Times.Once);
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid2), Times.Once);
        
        // Verify cache is working for each port independently
        var cachedResult1 = await _service.GetRegistersBySlaveIdAsync(port1, slaveAddress);
        var cachedResult2 = await _service.GetRegistersBySlaveIdAsync(port2, slaveAddress);
        
        Assert.Equal(registers1, cachedResult1);
        Assert.Equal(registers2, cachedResult2);
        
        // Still only one call each (cache hit for both ports)
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid1), Times.Once);
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveUuid2), Times.Once);
    }

    #endregion

    #region CreateRegisterAsync Tests

    [Fact]
    public async Task CreateRegisterAsync_ValidRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

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

        var expectedRegister = new Register
        {
            Id = "generated-register-id",
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        };
        _mockRegisterRepository.Setup(r => r.CreateAsync(It.IsAny<Register>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _service.CreateRegisterAsync(connectionId, slaveId, request);

        // Assert
        Assert.Equal(expectedRegister, result);
        _mockRegisterRepository.Verify(r => r.CreateAsync(It.Is<Register>(reg =>
            reg.Slaveid == slaveId && reg.Startaddr == 40001 && reg.Hexdata == "ABCD")), Times.Once);
    }

    [Theory]
    [InlineData("", "slave-id")]
    [InlineData(null, "slave-id")]
    [InlineData("   ", "slave-id")]
    public async Task CreateRegisterAsync_EmptyConnectionId_ShouldThrowArgumentException(string connectionId, string slaveId)
    {
        // Arrange
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Theory]
    [InlineData("connection-id", "")]
    [InlineData("connection-id", null)]
    [InlineData("connection-id", "   ")]
    public async Task CreateRegisterAsync_EmptySlaveId_ShouldThrowArgumentException(string connectionId, string slaveId)
    {
        // Arrange
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Fact]
    public async Task CreateRegisterAsync_NonExistentConnection_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "non-existent-connection";
        var slaveId = "slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(new List<ConnectionTree>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("连接不存在", exception.Message);
    }

    [Fact]
    public async Task CreateRegisterAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "non-existent-slave";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = "different-slave", Name = "Test Slave", Slaveid = 1 } }
            }
        };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("从机不存在", exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task CreateRegisterAsync_NegativeStartAddr_ShouldThrowArgumentException(int startAddr)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = startAddr, Hexdata = "ABCD" };

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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("起始地址不能为负数", exception.Message);
        Assert.Equal("request.Startaddr", exception.ParamName);
    }




    [Theory]
    [InlineData(40001, "ABCD")]       // Valid holding register
    [InlineData(30001, "ABCD")]       // Valid input register
    [InlineData(1, "AB")]             // Valid coil
    [InlineData(10001, "AB")]         // Valid discrete input
    public async Task CreateRegisterAsync_ValidHexdataLength_ShouldSucceed(int startAddr, string hexdata)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = startAddr, Hexdata = hexdata };

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

        var expectedRegister = new Register
        {
            Id = "generated-register-id",
            Slaveid = slaveId,
            Startaddr = startAddr,
            Hexdata = hexdata.ToUpper()
        };
        _mockRegisterRepository.Setup(r => r.CreateAsync(It.IsAny<Register>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _service.CreateRegisterAsync(connectionId, slaveId, request);

        // Assert
        Assert.Equal(expectedRegister, result);
        _mockRegisterRepository.Verify(r => r.CreateAsync(It.Is<Register>(reg =>
            reg.Hexdata == hexdata.ToUpper())), Times.Once);
    }

    [Theory]
    [InlineData(0)]       // Invalid: below range
    [InlineData(10000)]   // Invalid: between coil and discrete input
    [InlineData(20000)]   // Invalid: between discrete input and input register
    [InlineData(30000)]   // Invalid: between input register and holding register
    [InlineData(50000)]   // Invalid: above holding register range
    public async Task CreateRegisterAsync_InvalidAddressRange_ShouldThrowArgumentException(int startAddr)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = startAddr, Hexdata = "ABCD" };

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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("起始地址不在有效范围内", exception.Message);
        Assert.Equal("request.Startaddr", exception.ParamName);
    }

    [Fact]
    public async Task CreateRegisterAsync_DuplicateStartAddr_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

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

        _mockRegisterRepository.Setup(r => r.CreateAsync(It.IsAny<Register>()))
            .ThrowsAsync(new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed: registers.startaddr", 0));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));
        Assert.Contains("该从机下已存在相同起始地址的寄存器", exception.Message);
    }

    #endregion

    #region UpdateRegisterAsync Tests

    [Fact]
    public async Task UpdateRegisterAsync_ValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";
        var request = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "1234" };

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

        var expectedRegister = new Register
        {
            Id = registerId,
            Slaveid = slaveId,
            Startaddr = 40002,
            Hexdata = "1234"
        };
        _mockRegisterRepository.Setup(r => r.UpdateAsync(It.IsAny<Register>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request);

        // Assert
        Assert.Equal(expectedRegister, result);
        _mockRegisterRepository.Verify(r => r.UpdateAsync(It.Is<Register>(reg =>
            reg.Id == registerId && reg.Slaveid == slaveId && reg.Startaddr == 40002 && reg.Hexdata == "1234")), Times.Once);
    }

    [Theory]
    [InlineData("", "slave-id", "reg-id")]
    [InlineData(null, "slave-id", "reg-id")]
    [InlineData("   ", "slave-id", "reg-id")]
    public async Task UpdateRegisterAsync_EmptyConnectionId_ShouldThrowArgumentException_ForUpdate(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Theory]
    [InlineData("connection-id", "", "reg-id")]
    [InlineData("connection-id", null, "reg-id")]
    [InlineData("connection-id", "   ", "reg-id")]
    public async Task UpdateRegisterAsync_EmptySlaveId_ShouldThrowArgumentException_ForUpdate(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Theory]
    [InlineData("connection-id", "slave-id", "")]
    [InlineData("connection-id", "slave-id", null)]
    [InlineData("connection-id", "slave-id", "   ")]
    public async Task UpdateRegisterAsync_EmptyRegisterId_ShouldThrowArgumentException_ForUpdate(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("寄存器ID不能为空", exception.Message);
        Assert.Equal("registerId", exception.ParamName);
    }



    [Fact]
    public async Task UpdateRegisterAsync_NonExistentConnectionOrSlave_ShouldThrowKeyNotFound()
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync())
            .ReturnsAsync(new List<ConnectionTree>());

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.UpdateRegisterAsync("conn", "slave", "reg", request));
    }

    [Theory]
    [InlineData("", "slave-id", "register-id")]
    [InlineData(null, "slave-id", "register-id")]
    [InlineData("   ", "slave-id", "register-id")]
    public async Task UpdateRegisterAsync_EmptyConnectionId_ShouldThrowArgumentException(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("连接ID不能为空", exception.Message);
        Assert.Equal("connectionId", exception.ParamName);
    }

    [Theory]
    [InlineData("connection-id", "", "register-id")]
    [InlineData("connection-id", null, "register-id")]
    [InlineData("connection-id", "   ", "register-id")]
    public async Task UpdateRegisterAsync_EmptySlaveId_ShouldThrowArgumentException(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Theory]
    [InlineData("connection-id", "slave-id", "")]
    [InlineData("connection-id", "slave-id", null)]
    [InlineData("connection-id", "slave-id", "   ")]
    public async Task UpdateRegisterAsync_EmptyRegisterId_ShouldThrowArgumentException(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new UpdateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateRegisterAsync(connectionId, slaveId, registerId, request));
        Assert.Contains("寄存器ID不能为空", exception.Message);
        Assert.Equal("registerId", exception.ParamName);
    }

    #endregion

    #region DeleteRegisterAsync Tests

    [Fact]
    public async Task DeleteRegisterAsync_ValidIds_ShouldDeleteSuccessfully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";

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
        _mockRegisterRepository.Setup(r => r.DeleteAsync(registerId)).ReturnsAsync(true);

        // Act
        await _service.DeleteRegisterAsync(connectionId, slaveId, registerId);

        // Assert
        _mockRegisterRepository.Verify(r => r.DeleteAsync(registerId), Times.Once);
    }

    [Theory]
    [InlineData("", "slave", "reg", "connectionId")]
    [InlineData("conn", "", "reg", "slaveId")]
    [InlineData("conn", "slave", "", "registerId")]
    public async Task DeleteRegisterAsync_EmptyIds_ShouldThrowArgumentException(string connectionId, string slaveId, string registerId, string expectedParam)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.DeleteRegisterAsync(connectionId, slaveId, registerId));
        Assert.Equal(expectedParam, ex.ParamName);
    }

    [Fact]
    public async Task DeleteRegisterAsync_NonExistentSlave_ShouldThrowKeyNotFound()
    {
        // Arrange
        var connectionId = "conn";
        var slaveId = "missing-slave";
        var registerId = "reg";

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = connectionId,
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = "other-slave", Name = "Other", Slaveid = 2 } }
            }
        };
        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.DeleteRegisterAsync(connectionId, slaveId, registerId));
    }

    #endregion
}
