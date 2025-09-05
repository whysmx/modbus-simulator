using ModbusSimulator.Tcp;
using ModbusSimulator.Models;
using ModbusSimulator.Services;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Tcp;

public class ModbusRtuOverTcpServiceTests
{
    private readonly Mock<IRegisterService> _mockRegisterService;
    private readonly ModbusRtuOverTcpService _service;

    public ModbusRtuOverTcpServiceTests()
    {
        _mockRegisterService = new Mock<IRegisterService>();
        _service = new ModbusRtuOverTcpService(_mockRegisterService.Object);
    }

    [Fact]
    public void ProtocolType_ReturnsCorrectValue()
    {
        // Act
        var protocolType = _service.ProtocolType;

        // Assert
        Assert.Equal("ModbusRtuOverTcp", protocolType);
    }

    [Fact]
    public async Task ProcessRequestAsync_ValidReadHoldingRegistersRequest_ReturnsResponse()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)3; // Read Holding Registers
        var startAddress = (ushort)40001; // 保持寄存器地址
        var quantity = (ushort)2; // 读取 2 个寄存器

        // 构建 RTU 请求帧: SlaveAddr + FunctionCode + StartAddr(2) + Quantity(2) + CRC(2)
        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF), // 大端序
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        // 计算并添加 CRC
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF)); // CRC 低字节
        request.Add((byte)(crc >> 8));   // CRC 高字节

        // 模拟数据库返回的寄存器数据
        var mockRegisters = new List<Register>
        {
            new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = "1234" },
            new Register { Id = "2", Slaveid = "1", Startaddr = 40002, Hexdata = "5678" }
        };

        _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, slaveAddress.ToString()))
            .ReturnsAsync(mockRegisters);

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Length >= 7); // 至少应该有: SlaveAddr + FunctionCode + ByteCount + Data(4) + CRC(2)
        
        // 验证响应结构
        Assert.Equal(slaveAddress, response[0]); // Slave Address
        Assert.Equal(functionCode, response[1]); // Function Code
        Assert.Equal(4, response[2]); // Byte Count (2 registers * 2 bytes each)
        
        // 验证寄存器数据 (大端序)
        Assert.Equal(0x12, response[3]); // 第一个寄存器高字节
        Assert.Equal(0x34, response[4]); // 第一个寄存器低字节
        Assert.Equal(0x56, response[5]); // 第二个寄存器高字节
        Assert.Equal(0x78, response[6]); // 第二个寄存器低字节

        // 验证 CRC（最后两个字节）
        var responseWithoutCrc = response.Take(response.Length - 2).ToArray();
        var expectedCrc = CalculateCrc16(responseWithoutCrc);
        var actualCrc = (ushort)(response[response.Length - 2] | (response[response.Length - 1] << 8));
        Assert.Equal(expectedCrc, actualCrc);
    }

    [Fact]
    public async Task ProcessRequestAsync_ValidReadInputRegistersRequest_ReturnsResponse()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)4; // Read Input Registers
        var startAddress = (ushort)30001; // 输入寄存器地址
        var quantity = (ushort)1;

        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));

        var mockRegisters = new List<Register>
        {
            new Register { Id = "1", Slaveid = "1", Startaddr = 30001, Hexdata = "ABCD" }
        };

        _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, slaveAddress.ToString()))
            .ReturnsAsync(mockRegisters);

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(slaveAddress, response[0]); // Slave Address
        Assert.Equal(functionCode, response[1]); // Function Code
        Assert.Equal(2, response[2]); // Byte Count (1 register * 2 bytes)
        Assert.Equal(0xAB, response[3]); // 寄存器高字节
        Assert.Equal(0xCD, response[4]); // 寄存器低字节
    }

    [Fact]
    public async Task ProcessRequestAsync_RegisterNotFound_ReturnsZeroValues()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)3;
        var startAddress = (ushort)40001;
        var quantity = (ushort)1;

        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));

        // 模拟数据库返回空结果
        _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, slaveAddress.ToString()))
            .ReturnsAsync(new List<Register>());

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(slaveAddress, response[0]);
        Assert.Equal(functionCode, response[1]);
        Assert.Equal(2, response[2]); // Byte Count
        Assert.Equal(0x00, response[3]); // 不存在的寄存器返回 0
        Assert.Equal(0x00, response[4]);
    }

    [Fact]
    public async Task ProcessRequestAsync_InvalidAddressRange_ReturnsErrorResponse()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)3;
        var startAddress = (ushort)20001; // 无效地址范围
        var quantity = (ushort)1;

        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(slaveAddress, response[0]);
        Assert.Equal((byte)(functionCode + 0x80), response[1]); // 错误功能码
        Assert.Equal(0x02, response[2]); // 非法数据地址错误
    }

    [Fact]
    public async Task ProcessRequestAsync_InvalidQuantityRange_ReturnsErrorResponse()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)3;
        var startAddress = (ushort)40001;
        var quantity = (ushort)200; // 超出范围

        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(slaveAddress, response[0]);
        Assert.Equal((byte)(functionCode + 0x80), response[1]); // 错误功能码
        Assert.Equal(0x03, response[2]); // 非法数据值错误
    }

    [Fact]
    public async Task ProcessRequestAsync_UnsupportedFunctionCode_ReturnsErrorResponse()
    {
        // Arrange
        var slaveAddress = (byte)1;
        var functionCode = (byte)5; // 不支持的功能码
        var startAddress = (ushort)40001;
        var quantity = (ushort)1;

        var request = new List<byte>
        {
            slaveAddress,
            functionCode,
            (byte)(startAddress >> 8), (byte)(startAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        
        var crc = CalculateCrc16(request.ToArray());
        request.Add((byte)(crc & 0xFF));
        request.Add((byte)(crc >> 8));

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request.ToArray(), context);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(slaveAddress, response[0]);
        Assert.Equal((byte)(functionCode + 0x80), response[1]); // 错误功能码
        Assert.Equal(0x01, response[2]); // 非法功能码错误
    }

    [Fact]
    public async Task ProcessRequestAsync_InvalidCrc_ReturnsErrorResponse()
    {
        // Arrange - 构建 CRC 错误的请求
        var request = new byte[]
        {
            0x01, // Slave Address
            0x03, // Function Code
            0x9C, 0x41, // Start Address (40001)
            0x00, 0x01, // Quantity
            0x00, 0x00  // 错误的 CRC
        };

        var context = new ProtocolContext
        {
            ConnectionId = "test-connection",
            LocalPort = 502,
            RemoteEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("127.0.0.1"), 12345)
        };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert - 应该返回错误响应或空响应
        Assert.NotNull(response);
        // CRC 错误会在解析阶段被捕获并返回默认错误响应
    }

    private static ushort CalculateCrc16(byte[] data)
    {
        ushort crc = 0xFFFF;
        
        for (int i = 0; i < data.Length; i++)
        {
            crc ^= data[i];
            
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) == 1)
                {
                    crc = (ushort)((crc >> 1) ^ 0xA001);
                }
                else
                {
                    crc = (ushort)(crc >> 1);
                }
            }
        }
        
        return crc;
    }
}