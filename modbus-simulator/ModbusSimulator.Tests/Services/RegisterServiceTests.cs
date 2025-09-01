using ModbusSimulator.Models;
using ModbusSimulator.Repositories;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Services;

public class RegisterServiceTests
{
    private readonly Mock<IRegisterRepository> _mockRegisterRepository;
    private readonly Mock<IConnectionRepository> _mockConnectionRepository;
    private readonly Mock<ISlaveRepository> _mockSlaveRepository;
    private readonly RegisterService _service;

    public RegisterServiceTests()
    {
        _mockRegisterRepository = new Mock<IRegisterRepository>();
        _mockConnectionRepository = new Mock<IConnectionRepository>();
        _mockSlaveRepository = new Mock<ISlaveRepository>();
        _service = new RegisterService(
            _mockRegisterRepository.Object,
            _mockConnectionRepository.Object,
            _mockSlaveRepository.Object);
    }

    #region GetRegistersBySlaveIdAsync Tests

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_ValidSlaveId_ShouldReturnRegisters()
    {
        // Arrange
        var slaveId = "test-slave-id";
        var expectedRegisters = new List<Register>
        {
            new Register { Id = "reg1", Slaveid = slaveId, Startaddr = 40001, Hexdata = "ABCD" },
            new Register { Id = "reg2", Slaveid = slaveId, Startaddr = 40002, Hexdata = "1234" }
        };

        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = "connection-id",
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = slaveId, Name = "Test Slave", Slaveid = 1 } }
            }
        };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);
        _mockRegisterRepository.Setup(r => r.GetBySlaveIdAsync(slaveId)).ReturnsAsync(expectedRegisters);

        // Act
        var result = await _service.GetRegistersBySlaveIdAsync(slaveId);

        // Assert
        Assert.Equal(expectedRegisters, result);
        _mockRegisterRepository.Verify(r => r.GetBySlaveIdAsync(slaveId), Times.Once);
    }

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_EmptySlaveId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.GetRegistersBySlaveIdAsync(""));
        Assert.Contains("从机ID不能为空", exception.Message);
        Assert.Equal("slaveId", exception.ParamName);
    }

    [Fact]
    public async Task GetRegistersBySlaveIdAsync_NonExistentSlave_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var slaveId = "non-existent-slave";
        var connections = new List<ConnectionTree>
        {
            new ConnectionTree
            {
                Id = "connection-id",
                Name = "Test Connection",
                Port = 502,
                Slaves = new List<Slave> { new Slave { Id = "different-slave", Name = "Test Slave", Slaveid = 1 } }
            }
        };

        _mockConnectionRepository.Setup(r => r.GetConnectionsTreeAsync()).ReturnsAsync(connections);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetRegistersBySlaveIdAsync(slaveId));
        Assert.Contains("从机不存在", exception.Message);
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
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task CreateRegisterAsync_EmptyHexdata_ShouldThrowArgumentException(string hexdata)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = hexdata };

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
        Assert.Contains("十六进制数据不能为空", exception.Message);
        Assert.Equal("request.Hexdata", exception.ParamName);
    }

    [Theory]
    [InlineData("XYZ")]      // Invalid hex characters
    [InlineData("123")]      // Odd length
    [InlineData("12 34")]    // Contains spaces
    [InlineData("12@34")]    // Invalid characters
    public async Task CreateRegisterAsync_InvalidHexdata_ShouldThrowArgumentException(string hexdata)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = hexdata };

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
        Assert.Contains("十六进制数据格式无效", exception.Message);
        Assert.Equal("request.Hexdata", exception.ParamName);
    }

    [Theory]
    [InlineData(40001, "ABC")]        // Holding register: length should be multiple of 4
    [InlineData(30001, "ABC")]        // Input register: length should be multiple of 4
    [InlineData(1, "A")]              // Coil: length should be multiple of 2
    [InlineData(10001, "A")]          // Discrete input: length should be multiple of 2
    public async Task CreateRegisterAsync_InvalidHexdataLength_ShouldThrowArgumentException(int startAddr, string hexdata)
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

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(connectionId, slaveId, request));

        // 根据地址范围检查具体的错误信息
        if (startAddr >= 30001 && startAddr <= 39999) // 输入寄存器
        {
            Assert.Contains("输入寄存器数据长度必须是4的倍数", exception.Message);
        }
        else if (startAddr >= 40001 && startAddr <= 49999) // 保持寄存器
        {
            Assert.Contains("保持寄存器数据长度必须是4的倍数", exception.Message);
        }
        else if (startAddr >= 1 && startAddr <= 9999) // 线圈
        {
            Assert.Contains("线圈数据长度必须是2的倍数", exception.Message);
        }
        else if (startAddr >= 10001 && startAddr <= 19999) // 离散输入
        {
            Assert.Contains("离散输入数据长度必须是2的倍数", exception.Message);
        }

        Assert.Equal("Hexdata", exception.ParamName);
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

    #endregion

    #region Hex Validation Helper Tests

    [Theory]
    [InlineData("1234ABCD", true)]
    [InlineData("abcdef12", true)]
    [InlineData("ABCDEF12", true)]
    [InlineData("12ab34cd", true)]
    [InlineData("", false)]
    [InlineData("XYZ", false)]
    [InlineData("123", false)]      // Odd length
    [InlineData("12 34", false)]    // Contains space
    [InlineData("12@34", false)]    // Invalid character
    public void IsValidHexString_ShouldValidateCorrectly(string hexString, bool expected)
    {
        // Act
        var result = RegisterService.IsValidHexString(hexString);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(40001, "ABCD", true)]         // Holding register: 4 chars valid
    [InlineData(40001, "ABCD1234", true)]     // Holding register: 8 chars valid
    [InlineData(40001, "ABC", false)]         // Holding register: 3 chars invalid
    [InlineData(30001, "ABCD", true)]         // Input register: 4 chars valid
    [InlineData(30001, "ABC", false)]         // Input register: 3 chars invalid
    [InlineData(1, "AB", true)]               // Coil: 2 chars valid
    [InlineData(1, "A", false)]               // Coil: 1 char invalid
    [InlineData(10001, "ABCD", true)]         // Discrete input: 4 chars valid
    [InlineData(10001, "ABC", false)]         // Discrete input: 3 chars invalid
    public void ValidateHexDataLength_ShouldValidateCorrectly(int startAddr, string hexData, bool shouldPass)
    {
        // Act & Assert
        if (shouldPass)
        {
            // Should not throw
            RegisterService.ValidateHexDataLength(startAddr, hexData);
        }
        else
        {
            // Should throw
            Assert.Throws<ArgumentException>(() =>
                RegisterService.ValidateHexDataLength(startAddr, hexData));
        }
    }

    #endregion
}
