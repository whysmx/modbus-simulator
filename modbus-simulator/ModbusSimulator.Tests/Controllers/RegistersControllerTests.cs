using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Controllers;
using ModbusSimulator.Models;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Controllers;

public class RegistersControllerTests
{
    private readonly Mock<IRegisterService> _mockRegisterService;
    private readonly Mock<IConnectionService> _mockConnectionService;
    private readonly RegistersController _controller;

    public RegistersControllerTests()
    {
        _mockRegisterService = new Mock<IRegisterService>();
        _mockConnectionService = new Mock<IConnectionService>();
        _controller = new RegistersController(_mockRegisterService.Object, _mockConnectionService.Object);
    }

    #region GetRegisters Tests

    [Fact]
    public async Task GetRegisters_ValidIds_ShouldReturnOkResult()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var testPort = 502;
        var expectedRegisters = new List<Register>
        {
            new Register { Id = "reg1", Slaveid = slaveId, Startaddr = 40001, Hexdata = "ABCD" },
            new Register { Id = "reg2", Slaveid = slaveId, Startaddr = 40002, Hexdata = "1234" }
        };

        var mockConnection = new Connection { Id = connectionId, Port = testPort };
        _mockConnectionService.Setup(c => c.GetConnectionByIdAsync(connectionId)).ReturnsAsync(mockConnection);
        _mockRegisterService.Setup(r => r.GetRegistersBySlaveIdAsync(testPort, slaveId)).ReturnsAsync(expectedRegisters);

        // Act
        var result = await _controller.GetRegisters(connectionId, slaveId);

        // Assert
        var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedRegisters = okResult.Value as IEnumerable<Register>;
        Assert.Equal(expectedRegisters, returnedRegisters);
        _mockConnectionService.Verify(c => c.GetConnectionByIdAsync(connectionId), Times.Once);
        _mockRegisterService.Verify(r => r.GetRegistersBySlaveIdAsync(testPort, slaveId), Times.Once);
    }

    [Fact]
    public async Task GetRegisters_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var testPort = 502;

        var mockConnection = new Connection { Id = connectionId, Port = testPort };
        _mockConnectionService.Setup(c => c.GetConnectionByIdAsync(connectionId)).ReturnsAsync(mockConnection);
        _mockRegisterService.Setup(r => r.GetRegistersBySlaveIdAsync(testPort, slaveId))
            .ThrowsAsync(new KeyNotFoundException("Slave not found"));

        // Act
        var result = await _controller.GetRegisters(connectionId, slaveId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Slave not found", error.error);
        Assert.Equal(404, error.code);
    }

    [Fact]
    public async Task GetRegisters_EmptyResult_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var testPort = 502;
        var emptyRegisters = new List<Register>();

        var mockConnection = new Connection { Id = connectionId, Port = testPort };
        _mockConnectionService.Setup(c => c.GetConnectionByIdAsync(connectionId)).ReturnsAsync(mockConnection);
        _mockRegisterService.Setup(r => r.GetRegistersBySlaveIdAsync(testPort, slaveId)).ReturnsAsync(emptyRegisters);

        // Act
        var result = await _controller.GetRegisters(connectionId, slaveId);

        // Assert
        var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        var returnedRegisters = okResult.Value as IEnumerable<Register>;
        Assert.Empty(returnedRegisters!);
    }

    #endregion

    #region CreateRegister Tests

    [Fact]
    public async Task CreateRegister_ValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };
        var expectedRegister = new Register
        {
            Id = "generated-register-id",
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        };

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, request);

        // Assert
        var createdResult = Assert.IsAssignableFrom<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(expectedRegister, createdResult.Value);

        _mockRegisterService.Verify(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<CreateRegisterRequest>(req =>
            req.Startaddr == 40001 && req.Hexdata == "ABCD")), Times.Once);
    }

    [Fact]
    public async Task CreateRegister_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Slave not found"));

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Slave not found", error.error);
        Assert.Equal(404, error.code);
    }

    [Fact]
    public async Task CreateRegister_InvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate register"));

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Duplicate register", error.error);
        Assert.Equal(400, error.code);
    }

    #endregion

    #region UpdateRegister Tests

    [Fact]
    public async Task UpdateRegister_ValidRequest_ShouldReturnOkResult()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";
        var request = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "1234" };
        var expectedRegister = new Register
        {
            Id = registerId,
            Slaveid = slaveId,
            Startaddr = 40002,
            Hexdata = "1234"
        };

        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _controller.UpdateRegister(connectionId, slaveId, registerId, request);

        // Assert
        var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);
        Assert.Equal(expectedRegister, okResult.Value);

        _mockRegisterService.Verify(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.Is<UpdateRegisterRequest>(req =>
            req.Startaddr == 40002 && req.Hexdata == "1234")), Times.Once);
    }

    [Fact]
    public async Task UpdateRegister_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";
        var request = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "1234" };

        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Register not found"));

        // Act
        var result = await _controller.UpdateRegister(connectionId, slaveId, registerId, request);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Register not found", error.error);
        Assert.Equal(404, error.code);
    }

    [Fact]
    public async Task UpdateRegister_InvalidOperationException_ShouldReturnBadRequest()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";
        var request = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "1234" };

        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>()))
            .ThrowsAsync(new InvalidOperationException("Duplicate register"));

        // Act
        var result = await _controller.UpdateRegister(connectionId, slaveId, registerId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Duplicate register", error.error);
        Assert.Equal(400, error.code);
    }

    #endregion

    #region DeleteRegister Tests

    [Fact]
    public async Task DeleteRegister_ValidIds_ShouldReturnNoContent()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";

        // DeleteRegisterAsync returns Task, no setup needed

        // Act
        var result = await _controller.DeleteRegister(connectionId, slaveId, registerId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockRegisterService.Verify(r => r.DeleteRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), registerId), Times.Once);
    }

    [Fact]
    public async Task DeleteRegister_KeyNotFoundException_ShouldReturnNotFound()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";

        _mockRegisterService.Setup(r => r.DeleteRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("Register not found"));

        // Act
        var result = await _controller.DeleteRegister(connectionId, slaveId, registerId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(404, notFoundResult.StatusCode);
        var error = notFoundResult.Value as dynamic;
        Assert.Equal("Register not found", error.error);
        Assert.Equal(404, error.code);
    }

    #endregion

    #region Model Binding Tests

    [Fact]
    public async Task CreateRegister_NullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ReturnsAsync(new Register { Id = "test-id", Slaveid = slaveId, Startaddr = 0, Hexdata = null });

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, null);

        // Assert
        var badRequestResult = Assert.IsAssignableFrom<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task UpdateRegister_NullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var registerId = "test-register-id";

        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>()))
            .ReturnsAsync(new Register { Id = registerId, Slaveid = slaveId, Startaddr = 0, Hexdata = null });

        // Act
        var result = await _controller.UpdateRegister(connectionId, slaveId, registerId, null);

        // Assert
        var badRequestResult = Assert.IsAssignableFrom<BadRequestObjectResult>(result.Result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    #endregion

    #region Route Parameter Tests

    [Theory]
    [InlineData("valid-connection-id", "valid-slave-id", "valid-register-id")]
    [InlineData("123", "456", "789")]
    [InlineData("conn_123", "slave_456", "reg_789")]
    public async Task CreateRegister_VariousRouteParameters_ShouldPassThrough(string connectionId, string slaveId, string registerId)
    {
        // Arrange
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };
        var expectedRegister = new Register
        {
            Id = registerId, // Use the registerId parameter from test data
            Slaveid = slaveId,
            Startaddr = 40001,
            Hexdata = "ABCD"
        };

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ReturnsAsync(expectedRegister);

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, request);

        // Assert
        var createdResult = Assert.IsAssignableFrom<CreatedAtActionResult>(result.Result);
        var returnedRegister = createdResult.Value as Register;
        Assert.Equal(slaveId, returnedRegister.Slaveid);
    }

    #endregion

    #region HTTP Status Code Tests

    [Fact]
    public async Task GetRegisters_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test OK (200)
        var testPort = 502;
        var mockConnection = new Connection { Id = "conn", Port = testPort };
        var registers = new List<Register> { new Register { Id = "id", Slaveid = "slave", Startaddr = 40001, Hexdata = "ABCD" } };
        _mockConnectionService.Setup(c => c.GetConnectionByIdAsync("conn")).ReturnsAsync(mockConnection);
        _mockRegisterService.Setup(r => r.GetRegistersBySlaveIdAsync(testPort, "slave")).ReturnsAsync(registers);

        var result = await _controller.GetRegisters("conn", "slave");
        var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        // Test NotFound (404)
        _mockConnectionService.Setup(c => c.GetConnectionByIdAsync("conn")).ReturnsAsync(mockConnection);
        _mockRegisterService.Setup(r => r.GetRegistersBySlaveIdAsync(testPort, "slave"))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.GetRegisters("conn", "slave");
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Equal(404, notFoundObjectResult.StatusCode);
    }

    [Fact]
    public async Task CreateRegister_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test Created (201)
        var request = new CreateRegisterRequest { Startaddr = 40001, Hexdata = "ABCD" };
        var register = new Register { Id = "id", Slaveid = "slave", Startaddr = 40001, Hexdata = "ABCD" };
        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>())).ReturnsAsync(register);

        var result = await _controller.CreateRegister("conn", "slave", request);
        var createdResult = Assert.IsAssignableFrom<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);

        // Test NotFound (404)
        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.CreateRegister("conn", "slave", request);
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Equal(404, notFoundObjectResult.StatusCode);

        // Test BadRequest (400)
        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>()))
            .ThrowsAsync(new InvalidOperationException("Bad request"));
        var badRequestResult = await _controller.CreateRegister("conn", "slave", request);
        var badRequestObjectResult = Assert.IsType<BadRequestObjectResult>(badRequestResult.Result);
        Assert.Equal(400, badRequestObjectResult.StatusCode);
    }

    [Fact]
    public async Task UpdateRegister_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test OK (200)
        var request = new UpdateRegisterRequest { Startaddr = 40002, Hexdata = "1234" };
        var register = new Register { Id = "id", Slaveid = "slave", Startaddr = 40002, Hexdata = "1234" };
        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>())).ReturnsAsync(register);

        var result = await _controller.UpdateRegister("conn", "slave", "reg", request);
        var okResult = Assert.IsAssignableFrom<OkObjectResult>(result.Result);
        Assert.Equal(200, okResult.StatusCode);

        // Test NotFound (404)
        _mockRegisterService.Setup(r => r.UpdateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRegisterRequest>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.UpdateRegister("conn", "slave", "reg", request);
        var notFoundObjectResult = Assert.IsType<NotFoundObjectResult>(notFoundResult.Result);
        Assert.Equal(404, notFoundObjectResult.StatusCode);
    }

    [Fact]
    public async Task DeleteRegister_ShouldReturnCorrectHttpStatusCodes()
    {
        // Test NoContent (204)
        // DeleteRegisterAsync returns Task, no setup needed
        var result = await _controller.DeleteRegister("conn", "slave", "reg");
        Assert.IsType<NoContentResult>(result);

        // Test NotFound (404)
        _mockRegisterService.Setup(r => r.DeleteRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new KeyNotFoundException("Not found"));
        var notFoundResult = await _controller.DeleteRegister("conn", "slave", "reg");
        Assert.Equal(404, ((NotFoundObjectResult)notFoundResult).StatusCode);
    }

    #endregion

    #region Data Validation Tests

    [Theory]
    [InlineData(40001, "ABCD")]
    [InlineData(30001, "1234")]
    [InlineData(1, "FF")]
    [InlineData(10001, "00FF")]
    public async Task CreateRegister_VariousValidData_ShouldSucceed(int startaddr, string hexdata)
    {
        // Arrange
        var connectionId = "test-connection-id";
        var slaveId = "test-slave-id";
        var request = new CreateRegisterRequest { Startaddr = startaddr, Hexdata = hexdata };
        var expectedRegister = new Register
        {
            Id = "generated-id",
            Slaveid = slaveId,
            Startaddr = startaddr,
            Hexdata = hexdata
        };

        _mockRegisterService.Setup(r => r.CreateRegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateRegisterRequest>())).ReturnsAsync(expectedRegister);

        // Act
        var result = await _controller.CreateRegister(connectionId, slaveId, request);

        // Assert
        var createdResult = Assert.IsAssignableFrom<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
    }

    #endregion
}
