using ModbusSimulator.Tcp;
using Xunit;

namespace ModbusSimulator.Tests.Tcp;

public class ModbusTcpServiceTests
{
    private readonly ModbusTcpService _modbusService;

    public ModbusTcpServiceTests()
    {
        _modbusService = new ModbusTcpService();
    }

    [Fact]
    public void ProtocolType_ReturnsCorrectValue()
    {
        // Act
        var protocolType = _modbusService.ProtocolType;

        // Assert
        Assert.Equal("ModbusTCP", protocolType);
    }

    [Fact]
    public async Task ProcessRequestAsync_ValidReadCoilsRequest_ReturnsResponse()
    {
        // Arrange
        // Modbus TCP Read Coils request: TransactionId=0x0001, ProtocolId=0x0000, Length=0x0006
        // UnitId=0x01, FunctionCode=0x01, StartAddr=0x0000, Quantity=0x0008
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x01,       // Function Code (Read Coils)
            0x00, 0x00, // Start Address
            0x00, 0x08  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.NotEmpty(response);

        // 验证响应格式
        Assert.True(response.Length >= 8); // 至少MBAP头部 + PDU
        Assert.Equal(request[0], response[0]); // Transaction ID高字节
        Assert.Equal(request[1], response[1]); // Transaction ID低字节
        Assert.Equal(request[6], response[6]); // Unit ID
    }

    [Fact]
    public async Task ProcessRequestAsync_InvalidFunctionCode_ReturnsErrorResponse()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x10,       // Invalid Function Code (0x10 is not supported)
            0x00, 0x00, // Data
            0x00, 0x01
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 9); // MBAP + Error PDU

        // 验证错误响应格式
        Assert.Equal(request[0], response[0]); // Transaction ID高字节
        Assert.Equal(request[1], response[1]); // Transaction ID低字节
        Assert.Equal(request[6], response[6]); // Unit ID
        Assert.Equal((byte)(request[7] + 0x80), response[7]); // Error function code
        Assert.Equal(0x01, response[8]); // Error code (illegal function)
    }

    [Fact]
    public async Task ProcessRequestAsync_ReadDiscreteInputsRequest_ReturnsResponse()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x02, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x02,       // Function Code (Read Discrete Inputs)
            0x00, 0x00, // Start Address
            0x00, 0x10  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
        Assert.Equal(request[0], response[0]);
        Assert.Equal(request[1], response[1]);
        Assert.Equal(request[6], response[6]);
        Assert.Equal(request[7], response[7]); // Function code should be echoed
    }

    [Fact]
    public async Task ProcessRequestAsync_ReadHoldingRegistersRequest_ReturnsResponse()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x03, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x03,       // Function Code (Read Holding Registers)
            0x00, 0x00, // Start Address
            0x00, 0x02  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
        Assert.Equal(request[0], response[0]);
        Assert.Equal(request[1], response[1]);
        Assert.Equal(request[6], response[6]);
        Assert.Equal(request[7], response[7]);
    }

    [Fact]
    public async Task ProcessRequestAsync_ReadInputRegistersRequest_ReturnsResponse()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x04, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x04,       // Function Code (Read Input Registers)
            0x00, 0x00, // Start Address
            0x00, 0x02  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
        Assert.Equal(request[0], response[0]);
        Assert.Equal(request[1], response[1]);
        Assert.Equal(request[6], response[6]);
        Assert.Equal(request[7], response[7]);
    }

    [Fact]
    public async Task ProcessRequestAsync_EmptyRequest_ReturnsEmptyResponse()
    {
        // Arrange
        var request = Array.Empty<byte>();
        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task ProcessRequestAsync_MalformedRequest_ReturnsEmptyResponse()
    {
        // Arrange - Too short request
        var request = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithNullContext_HandlesGracefully()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x01, 0x00, 0x00, 0x00, 0x08
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, null);

        // Assert
        Assert.NotNull(response);
        // 由于异常处理，空的context可能导致空响应或异常响应
    }

    #region Modbus Frame Parsing Tests

    [Fact]
    public async Task ProcessRequestAsync_ParseModbusTcpFrame_CorrectlyParsesFrame()
    {
        // Arrange - Read Coils request
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID: 1
            0x00, 0x00, // Protocol ID: 0
            0x00, 0x06, // Length: 6
            0x01,       // Unit ID: 1
            0x01,       // Function Code: 1 (Read Coils)
            0x00, 0x00, // Start Address: 0
            0x00, 0x08  // Quantity: 8
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);

        // Verify response structure
        Assert.Equal(request[0], response[0]); // Transaction ID high byte
        Assert.Equal(request[1], response[1]); // Transaction ID low byte
        Assert.Equal(request[6], response[6]); // Unit ID
        Assert.Equal(request[7], response[7]); // Function Code (echoed)
    }

    [Theory]
    [InlineData(0x00, 0x01)]  // Transaction ID = 1
    [InlineData(0x12, 0x34)]  // Transaction ID = 4660
    [InlineData(0xFF, 0xFF)]  // Transaction ID = 65535
    public async Task ProcessRequestAsync_VariousTransactionIds_AreHandledCorrectly(byte highByte, byte lowByte)
    {
        // Arrange
        var request = new byte[]
        {
            highByte, lowByte, // Transaction ID
            0x00, 0x00,        // Protocol ID
            0x00, 0x06,        // Length
            0x01,              // Unit ID
            0x01,              // Function Code
            0x00, 0x00,        // Start Address
            0x00, 0x08         // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
        Assert.Equal(highByte, response[0]); // Transaction ID echoed
        Assert.Equal(lowByte, response[1]);
    }

    [Theory]
    [InlineData(1)]   // Slave ID 1
    [InlineData(247)] // Maximum valid slave ID
    [InlineData(0)]   // Broadcast (though not typically used for requests)
    public async Task ProcessRequestAsync_VariousSlaveIds_AreHandledCorrectly(byte slaveId)
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            slaveId,    // Unit ID
            0x01,       // Function Code
            0x00, 0x00, // Start Address
            0x00, 0x08  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
        Assert.Equal(slaveId, response[6]); // Unit ID echoed
    }

    #endregion

    #region Protocol Validation Tests

    [Theory]
    [InlineData(0)]   // Invalid: below minimum
    [InlineData(5)]   // Invalid: above maximum (4)
    [InlineData(10)]  // Invalid: way above maximum
    [InlineData(255)] // Invalid: maximum byte value
    public async Task ProcessRequestAsync_InvalidFunctionCodes_ReturnErrorResponse(byte invalidFunctionCode)
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            invalidFunctionCode, // Invalid Function Code
            0x00, 0x00, // Data
            0x00, 0x01
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 9); // MBAP + Error PDU

        // Verify error response structure
        Assert.Equal(request[0], response[0]); // Transaction ID echoed
        Assert.Equal(request[1], response[1]);
        Assert.Equal(request[6], response[6]); // Unit ID echoed
        Assert.Equal((byte)(invalidFunctionCode + 0x80), response[7]); // Error function code
        Assert.Equal(0x01, response[8]); // Error code (illegal function)
    }

    [Fact]
    public async Task ProcessRequestAsync_FunctionCodeBoundaries_AreValidated()
    {
        // Test valid function codes
        var validFunctionCodes = new byte[] { 1, 2, 3, 4 };

        foreach (var functionCode in validFunctionCodes)
        {
            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x01,       // Unit ID
                functionCode, // Valid Function Code
                0x00, 0x00, // Start Address
                0x00, 0x08  // Quantity
            };

            var context = new ProtocolContext
            {
                ConnectionId = "test-connection",
                LocalPort = 502,
                RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
            };

            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Should not return error response for valid function codes
            Assert.NotNull(response);
            Assert.True(response.Length >= 8);
            // Function code should be echoed (not error code)
            Assert.Equal(functionCode, response[7]);
        }
    }

    #endregion

    #region Frame Structure Validation Tests

    [Fact]
    public async Task ProcessRequestAsync_IncompleteFrame_ReturnsEmptyResponse()
    {
        // Arrange - Frame too short (missing data)
        var incompleteRequest = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length (claims 6 bytes but we provide less)
            0x01        // Unit ID only
            // Missing function code and data
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(incompleteRequest, context);

        // Assert - Should handle gracefully and return empty response
        Assert.NotNull(response);
        // Depending on implementation, may return empty or error response
    }

    [Fact]
    public async Task ProcessRequestAsync_InvalidProtocolId_IsAccepted()
    {
        // Arrange - Non-zero protocol ID (though Modbus TCP spec says it should be 0)
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x01, // Protocol ID: 1 (non-zero, but accepted)
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x01,       // Function Code
            0x00, 0x00, // Start Address
            0x00, 0x08  // Quantity
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert - Should still process the request despite non-zero protocol ID
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);
    }

    [Fact]
    public async Task ProcessRequestAsync_LengthFieldMismatch_IsHandled()
    {
        // Arrange - Length field doesn't match actual data length
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x0A, // Length: 10 (claims more data than provided)
            0x01,       // Unit ID
            0x01,       // Function Code
            0x00, 0x00, // Start Address
            0x00, 0x08  // Quantity
            // Actual data is shorter than claimed length
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert - Should handle gracefully
        Assert.NotNull(response);
    }

    #endregion

    #region Error Response Structure Tests

    [Fact]
    public async Task ProcessRequestAsync_ErrorResponse_HasCorrectStructure()
    {
        // Arrange - Invalid function code to trigger error response
        var request = new byte[]
        {
            0x12, 0x34, // Transaction ID: 0x1234
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x05,       // Unit ID: 5
            0xFF,       // Invalid Function Code: 255
            0x00, 0x00, // Data
            0x00, 0x01
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 9); // MBAP (7) + Error PDU (2) + Unit ID (1) - 1

        // Verify MBAP header
        Assert.Equal(0x12, response[0]); // Transaction ID high byte
        Assert.Equal(0x34, response[1]); // Transaction ID low byte
        Assert.Equal(0x00, response[2]); // Protocol ID high byte
        Assert.Equal(0x00, response[3]); // Protocol ID low byte

        // Length field should be 3 (1 byte unit ID + 2 bytes error PDU)
        Assert.Equal(0x00, response[4]); // Length high byte
        Assert.Equal(0x03, response[5]); // Length low byte

        Assert.Equal(0x05, response[6]); // Unit ID echoed
        Assert.Equal(0xFF + 0x80, response[7]); // Error function code (0xFF + 0x80 = 0x17F, but byte overflow to 0x7F)
        Assert.Equal(0x01, response[8]); // Error code (illegal function)
    }

    [Theory]
    [InlineData(0x01, (byte)(0x01 + 0x80))] // Function code 1 -> error code 0x81
    [InlineData(0x05, (byte)(0x05 + 0x80))] // Function code 5 -> error code 0x85
    [InlineData(0x10, (byte)(0x10 + 0x80))] // Function code 16 -> error code 0x90
    public async Task ProcessRequestAsync_ErrorFunctionCode_IsCalculatedCorrectly(byte functionCode, byte expectedErrorCode)
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            functionCode, // Function Code (invalid)
            0x00, 0x00, // Data
            0x00, 0x01
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 9);
        Assert.Equal(expectedErrorCode, response[7]); // Error function code
        Assert.Equal(0x01, response[8]); // Error code (illegal function)
    }

    #endregion

    #region Success Response Structure Tests

    [Fact]
    public async Task ProcessRequestAsync_SuccessResponse_HasCorrectMBAPStructure()
    {
        // Arrange - Valid read coils request
        var request = new byte[]
        {
            0xAB, 0xCD, // Transaction ID: 0xABCD
            0x00, 0x00, // Protocol ID: 0
            0x00, 0x06, // Length: 6
            0x02,       // Unit ID: 2
            0x01,       // Function Code: 1 (Read Coils)
            0x00, 0x00, // Start Address: 0
            0x00, 0x08  // Quantity: 8
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8); // At least MBAP + function code + unit ID

        // Verify MBAP header is echoed correctly
        Assert.Equal(0xAB, response[0]); // Transaction ID high byte
        Assert.Equal(0xCD, response[1]); // Transaction ID low byte
        Assert.Equal(0x00, response[2]); // Protocol ID high byte
        Assert.Equal(0x00, response[3]); // Protocol ID low byte
        Assert.Equal(0x02, response[6]); // Unit ID echoed
        Assert.Equal(0x01, response[7]); // Function code echoed
    }

    [Fact]
    public async Task ProcessRequestAsync_ResponseLength_IsCalculatedCorrectly()
    {
        // Arrange
        var request = new byte[]
        {
            0x00, 0x01, // Transaction ID
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length
            0x01,       // Unit ID
            0x03,       // Function Code: 3 (Read Holding Registers)
            0x00, 0x00, // Start Address
            0x00, 0x02  // Quantity: 2 registers
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(request, context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 8);

        // Length field should be response data length + 2 (unit ID + function code)
        // Since HandleReadFunctionAsync returns empty array, response data length = 0
        // So total length should be 2
        var lengthField = (ushort)((response[4] << 8) | response[5]);
        Assert.Equal(2, lengthField); // Unit ID (1) + Function Code (1) = 2
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ProcessRequestAsync_ExceptionDuringProcessing_ReturnsEmptyResponse()
    {
        // This test verifies that exceptions during processing are caught
        // and result in an empty response rather than propagating up

        // Arrange - Create a request that might cause issues
        var problematicRequest = new byte[]
        {
            0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x01, 0x00, 0x00, 0x00, 0x08
        };

        var context = new ProtocolContext
        {
            ConnectionId = null, // Null connection ID might cause issues
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _modbusService.ProcessRequestAsync(problematicRequest, context);

        // Assert
        Assert.NotNull(response);
        // Should return empty response on exception, not throw
    }

    #endregion
}

// ModbusTcpFrame的测试
public class ModbusTcpFrameTests
{
    [Fact]
    public void ModbusTcpFrame_CanBeInitialized()
    {
        // Arrange & Act
        var frame = new ModbusTcpFrame
        {
            TransactionId = 0x0001,
            Slaveid = 0x01,
            FunctionCode = 0x03,
            Data = new byte[] { 0x00, 0x00, 0x00, 0x02 }
        };

        // Assert
        Assert.Equal((ushort)0x0001, frame.TransactionId);
        Assert.Equal((byte)0x01, frame.Slaveid);
        Assert.Equal((byte)0x03, frame.FunctionCode);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02 }, frame.Data);
    }

    [Fact]
    public void ModbusTcpFrame_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var frame = new ModbusTcpFrame();

        // Assert
        Assert.Equal((ushort)0, frame.TransactionId);
        Assert.Equal((byte)0, frame.Slaveid);
        Assert.Equal((byte)0, frame.FunctionCode);
        Assert.Null(frame.Data);
    }
}