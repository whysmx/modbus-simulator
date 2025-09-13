using ModbusSimulator.Models;
using ModbusSimulator.Services;
using ModbusSimulator.Tcp;
using Moq;

namespace ModbusSimulator.Tests.Tcp;

public class ModbusTcpServiceAdvancedTests
{
    private readonly Mock<IRegisterService> _mockRegisterService;
    private readonly ModbusTcpService _service;

    public ModbusTcpServiceAdvancedTests()
    {
        _mockRegisterService = new Mock<IRegisterService>(MockBehavior.Strict);
        _service = new ModbusTcpService(_mockRegisterService.Object);
    }

    private static byte[] BuildTcpRequest(ushort transactionId, byte unitId, byte function, ushort start, ushort quantity)
    {
        var bytes = new List<byte>
        {
            (byte)(transactionId >> 8), (byte)(transactionId & 0xFF),
            0x00, 0x00, // Protocol ID
            0x00, 0x06, // Length (Unit + Function + Start(2) + Qty(2))
            unitId,
            function,
            (byte)(start >> 8), (byte)(start & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        return bytes.ToArray();
    }

    [Fact]
    public async Task ReadCoils_NonContiguous_WithBitPacking_Works()
    {
        // Arrange: coils at 1, 3, 8 are ON; range start logical 1 (start=0) read 9 bits => 2 bytes: 0x85, 0x00
        var registers = new List<Register>
        {
            // Order matters due to masking in implementation: put higher addresses first
            new Register { Slaveid = "1", Startaddr = 8, Hexdata = "1F" },
            new Register { Slaveid = "1", Startaddr = 3, Hexdata = "1F" },
            new Register { Slaveid = "1", Startaddr = 1, Hexdata = "1F" }
        };

        _mockRegisterService
            .Setup(s => s.GetRegistersBySlaveIdAsync(502, "1"))
            .ReturnsAsync(registers);

        var request = BuildTcpRequest(0x0001, 0x01, 0x01, 0x0000, 0x0009);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert
        Assert.Equal(0x01, response[7]); // function code
        Assert.Equal(2, response[8]);    // byte count for 9 bits
        Assert.Equal(0x85, response[9]); // bits 0,2,7 set
        Assert.Equal(0x00, response[10]);
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ReadDiscreteInputs_OffsetStart_NonContiguous_Works()
    {
        // Arrange: start protocol 5 -> logical 10006; set 10006 and 10008 = true; quantity 5 -> one byte 0x05
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 10008, Hexdata = "1F" },
            new Register { Slaveid = "1", Startaddr = 10006, Hexdata = "1F" }
        };

        _mockRegisterService
            .Setup(s => s.GetRegistersBySlaveIdAsync(502, "1"))
            .ReturnsAsync(registers);

        var request = BuildTcpRequest(0x0002, 0x01, 0x02, 0x0005, 0x0005);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert
        Assert.Equal(0x02, response[7]);
        Assert.Equal(1, response[8]);
        Assert.Equal(0x05, response[9]); // bit0, bit2
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ReadHoldingRegisters_OverlapAndGaps_AreHandled()
    {
        // Arrange: r1 40001: 1122 3344; r2 40002: AAAA (overrides address 40002)
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 40001, Hexdata = "11 22 33 44" },
            new Register { Slaveid = "1", Startaddr = 40002, Hexdata = "AA AA" }
        };

        _mockRegisterService
            .Setup(s => s.GetRegistersBySlaveIdAsync(502, "1"))
            .ReturnsAsync(registers);

        // start=0 -> logical 40001, quantity=3
        var request = BuildTcpRequest(0x0003, 0x01, 0x03, 0x0000, 0x0003);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert
        Assert.Equal(0x03, response[7]);
        Assert.Equal(6, response[8]);
        Assert.Equal(new byte[] { 0x11, 0x22, 0xAA, 0xAA, 0x00, 0x00 }, response.Skip(9).Take(6).ToArray());
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ReadInputRegisters_NonContiguous_MultipleSegments()
    {
        // Arrange: data at 30005=1234 and 30007=ABCD, read start protocol 4 (->30005), qty=4
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 30005, Hexdata = "12 34" },
            new Register { Slaveid = "1", Startaddr = 30007, Hexdata = "AB CD" }
        };

        _mockRegisterService
            .Setup(s => s.GetRegistersBySlaveIdAsync(502, "1"))
            .ReturnsAsync(registers);

        var request = BuildTcpRequest(0x0004, 0x01, 0x04, 0x0004, 0x0004);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert => [1234, 0000, ABCD, 0000]
        Assert.Equal(0x04, response[7]);
        Assert.Equal(8, response[8]);
        var data = response.Skip(9).Take(8).ToArray();
        Assert.Equal(new byte[] { 0x12, 0x34, 0x00, 0x00, 0xAB, 0xCD, 0x00, 0x00 }, data);
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ReadCoils_SingleBit_ReadsCorrectly()
    {
        // Arrange: read single coil at logical 10 (protocol start=10 per service's direct mapping rule)
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 10, Hexdata = "1F" }
        };

        _mockRegisterService
            .Setup(s => s.GetRegistersBySlaveIdAsync(502, "1"))
            .ReturnsAsync(registers);

        var request = BuildTcpRequest(0x0101, 0x01, 0x01, 0x000A, 0x0001);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert
        Assert.Equal(0x01, response[7]);
        Assert.Equal(1, response[8]);
        Assert.Equal(0x01, response[9]);
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ConcurrentRequests_MultipleClients_NoExceptions()
    {
        // Arrange two slaves with simple maps
        var registers1 = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 40001, Hexdata = "12 34" }
        };
        var registers2 = new List<Register>
        {
            new Register { Slaveid = "2", Startaddr = 40001, Hexdata = "56 78" }
        };

        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "1")).ReturnsAsync(registers1);
        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "2")).ReturnsAsync(registers2);

        var req1 = BuildTcpRequest(0x1001, 0x01, 0x03, 0x0000, 0x0001);
        var req2 = BuildTcpRequest(0x1002, 0x02, 0x03, 0x0000, 0x0001);
        var ctx = new ProtocolContext { LocalPort = 502 };

        // Act
        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            var request = (i % 2 == 0) ? req1 : req2;
            var response = await _service.ProcessRequestAsync(request, ctx);
            Assert.NotEmpty(response);
            // Verify payload's first two data bytes per slave
            if ((i % 2) == 0)
            {
                Assert.Equal(0x03, response[7]);
                Assert.Equal(2, response[8]);
                Assert.Equal(0x12, response[9]);
                Assert.Equal(0x34, response[10]);
            }
            else
            {
                Assert.Equal(0x03, response[7]);
                Assert.Equal(2, response[8]);
                Assert.Equal(0x56, response[9]);
                Assert.Equal(0x78, response[10]);
            }
        });

        await Task.WhenAll(tasks);
        _mockRegisterService.VerifyAll();
    }
}


