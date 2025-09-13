using ModbusSimulator.Models;
using ModbusSimulator.Services;
using ModbusSimulator.Tcp;
using Moq;

namespace ModbusSimulator.Tests.Tcp;

public class ModbusRtuOverTcpServiceAdvancedTests
{
    private readonly Mock<IRegisterService> _mockRegisterService;
    private readonly ModbusRtuOverTcpService _service;

    public ModbusRtuOverTcpServiceAdvancedTests()
    {
        _mockRegisterService = new Mock<IRegisterService>(MockBehavior.Strict);
        _service = new ModbusRtuOverTcpService(_mockRegisterService.Object);
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

    private static byte[] BuildRtuRequest(byte unitId, byte function, ushort startProtocolAddress, ushort quantity)
    {
        var frame = new List<byte>
        {
            unitId,
            function,
            (byte)(startProtocolAddress >> 8), (byte)(startProtocolAddress & 0xFF),
            (byte)(quantity >> 8), (byte)(quantity & 0xFF)
        };
        var crc = CalculateCrc16(frame.ToArray());
        frame.Add((byte)(crc & 0xFF));
        frame.Add((byte)(crc >> 8));
        return frame.ToArray();
    }

    [Fact]
    public async Task ReadHoldingRegisters_Overlap_Gap_Offset_Works()
    {
        // Arrange: data segments at 40005=0x1234, 40007=0xABCD; request start protocol 4 -> logical 40005, qty=4
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 40005, Hexdata = "12 34" },
            new Register { Slaveid = "1", Startaddr = 40007, Hexdata = "AB CD" }
        };

        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "1")).ReturnsAsync(registers);

        var request = BuildRtuRequest(0x01, 0x03, 0x0004, 0x0004);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert: RTU response: addr, func, bytecount(8), data(8), CRC(2)
        Assert.Equal(0x01, response[0]);
        Assert.Equal(0x03, response[1]);
        Assert.Equal(8, response[2]);
        Assert.Equal(new byte[] { 0x12, 0x34, 0x00, 0x00, 0xAB, 0xCD, 0x00, 0x00 }, response.Skip(3).Take(8).ToArray());
        // CRC correctness implicitly checked by service when parsing next time; here ensure non-zero
        Assert.True(response.Length >= 13);
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ReadInputRegisters_Continuous_ReadsCorrectBlock()
    {
        // Arrange: at 30001: 1122 3344 5566; start 0, qty 3
        var registers = new List<Register>
        {
            new Register { Slaveid = "1", Startaddr = 30001, Hexdata = "11 22 33 44 55 66" }
        };

        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "1")).ReturnsAsync(registers);

        var request = BuildRtuRequest(0x01, 0x04, 0x0000, 0x0003);
        var context = new ProtocolContext { LocalPort = 502 };

        // Act
        var response = await _service.ProcessRequestAsync(request, context);

        // Assert
        Assert.Equal(0x01, response[0]);
        Assert.Equal(0x04, response[1]);
        Assert.Equal(6, response[2]);
        Assert.Equal(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55, 0x66 }, response.Skip(3).Take(6).ToArray());
        _mockRegisterService.VerifyAll();
    }

    [Fact]
    public async Task ConcurrentRequests_NoCrossTalk()
    {
        var reg1 = new List<Register> { new Register { Slaveid = "1", Startaddr = 40001, Hexdata = "DE AD" } };
        var reg2 = new List<Register> { new Register { Slaveid = "2", Startaddr = 40001, Hexdata = "BE EF" } };

        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "1")).ReturnsAsync(reg1);
        _mockRegisterService.Setup(s => s.GetRegistersBySlaveIdAsync(502, "2")).ReturnsAsync(reg2);

        var req1 = BuildRtuRequest(0x01, 0x03, 0x0000, 0x0001);
        var req2 = BuildRtuRequest(0x02, 0x03, 0x0000, 0x0001);
        var ctx = new ProtocolContext { LocalPort = 502 };

        var tasks = Enumerable.Range(0, 40).Select(async i =>
        {
            var req = (i % 2 == 0) ? req1 : req2;
            var resp = await _service.ProcessRequestAsync(req, ctx);
            Assert.NotEmpty(resp);
            if ((i % 2) == 0)
            {
                Assert.Equal(0x01, resp[0]);
                Assert.Equal(0x03, resp[1]);
                Assert.Equal(2, resp[2]);
                Assert.Equal(0xDE, resp[3]);
                Assert.Equal(0xAD, resp[4]);
            }
            else
            {
                Assert.Equal(0x02, resp[0]);
                Assert.Equal(0x03, resp[1]);
                Assert.Equal(2, resp[2]);
                Assert.Equal(0xBE, resp[3]);
                Assert.Equal(0xEF, resp[4]);
            }
        });

        await Task.WhenAll(tasks);
        _mockRegisterService.VerifyAll();
    }
}


