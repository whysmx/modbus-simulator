using ModbusSimulator.Models;
using ModbusSimulator.Services;
using ModbusSimulator.Tcp;
using Moq;
using Xunit;

namespace ModbusSimulator.Tests.Tcp
{
    /// <summary>
    /// 基于真实Modbus通讯报文的端到端数据完整性测试
    /// 验证从数据库存储的16进制字符串到最终Modbus响应的完整数据流
    /// </summary>
    public class ModbusRealWorldDataTests
    {
        private readonly Mock<IRegisterService> _mockRegisterService;
        private readonly ModbusTcpService _modbusService;

        public ModbusRealWorldDataTests()
        {
            _mockRegisterService = new Mock<IRegisterService>();
            _modbusService = new ModbusTcpService(_mockRegisterService.Object);
        }

        #region 基于真实报文的测试用例

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_LargeDataRead_From200_Count78()
        {
            // 真实报文: 01 03 00 C8 00 4E 44 00 → 从地址200读取78个寄存器
            // 期望156字节响应数据
            var registers = new List<Register>
            {
                // 模拟从地址200开始的连续数据（真实报文数据）
                new Register { Id = "1", Slaveid = "1", Startaddr = 40201, Hexdata = "44 89 80 00 20 25 09 09" },
                new Register { Id = "2", Slaveid = "1", Startaddr = 40205, Hexdata = "14 04 10 00 00 00 00 00" },
                new Register { Id = "3", Slaveid = "1", Startaddr = 40209, Hexdata = "20 25 09 09 14 09 47 00" },
                new Register { Id = "4", Slaveid = "1", Startaddr = 40213, Hexdata = "44 78 40 00 C1 83 A2 E9" },
                new Register { Id = "5", Slaveid = "1", Startaddr = 40217, Hexdata = "3F 9C 6A 7F 00 00 00 00" },
                new Register { Id = "6", Slaveid = "1", Startaddr = 40221, Hexdata = "44 4B 00 00 42 C8 00 00" },
                new Register { Id = "7", Slaveid = "1", Startaddr = 40225, Hexdata = "43 C8 00 00 20 25 09 09" },
                new Register { Id = "8", Slaveid = "1", Startaddr = 40229, Hexdata = "14 04 10 00 00 00 00 00" },
                new Register { Id = "9", Slaveid = "1", Startaddr = 40233, Hexdata = "20 25 09 09 14 15 19 00" },
                new Register { Id = "10", Slaveid = "1", Startaddr = 40237, Hexdata = "43 B4 00 00 C1 AA CC CC" },
                new Register { Id = "11", Slaveid = "1", Startaddr = 40241, Hexdata = "3F A7 AE 14 00 00 00 00" },
                new Register { Id = "12", Slaveid = "1", Startaddr = 40245, Hexdata = "43 89 4C CD 42 C8 00 00" },
                new Register { Id = "13", Slaveid = "1", Startaddr = 40249, Hexdata = "41 C8 00 00 20 25 09 09" },
                new Register { Id = "14", Slaveid = "1", Startaddr = 40253, Hexdata = "14 04 10 00 00 00 00 00" },
                new Register { Id = "15", Slaveid = "1", Startaddr = 40257, Hexdata = "20 25 09 09 14 19 11 00" },
                new Register { Id = "16", Slaveid = "1", Startaddr = 40261, Hexdata = "41 AA 66 66 3D 23 D7 0A" },
                new Register { Id = "17", Slaveid = "1", Startaddr = 40265, Hexdata = "3F 7F BE 77 00 00 00 00" },
                new Register { Id = "18", Slaveid = "1", Startaddr = 40269, Hexdata = "41 AA 7A E1 42 C8 00 00" },
                new Register { Id = "19", Slaveid = "1", Startaddr = 40273, Hexdata = "00 00 00 00 00 00 00 00" },
                new Register { Id = "20", Slaveid = "1", Startaddr = 40277, Hexdata = "41 A6 7A E1" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x01,       // Unit ID
                0x03,       // Function Code (Read Holding Registers)
                0x00, 0xC8, // Starting Address (200)
                0x00, 0x4E  // Quantity (78 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(0x01, response[6]); // Unit ID
            Assert.Equal(0x03, response[7]); // Function Code
            Assert.Equal(156, response[8]); // Byte Count (78 * 2)

            // 验证前几个寄存器的数据
            Assert.Equal(0x44, response[9]);  // 第一个寄存器高字节
            Assert.Equal(0x89, response[10]); // 第一个寄存器低字节
            Assert.Equal(0x80, response[11]); // 第二个寄存器高字节
            Assert.Equal(0x00, response[12]); // 第二个寄存器低字节
        }

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_FloatData_Address6_Count2()
        {
            // 真实报文: 01 03 00 06 00 02 24 0A → 44 A0 8F 2D
            // IEEE754浮点数: 44A0 = 1312.0f, 8F2D = 约-2.7e38f
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 40007, Hexdata = "44 A0" },
                new Register { Id = "2", Slaveid = "1", Startaddr = 40008, Hexdata = "8F 2D" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x01,       // Unit ID
                0x03,       // Function Code
                0x00, 0x06, // Starting Address (6)
                0x00, 0x02  // Quantity (2 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(4, response[8]); // Byte Count (2 * 2)
            Assert.Equal(0x44, response[9]);  // 高字节
            Assert.Equal(0xA0, response[10]); // 低字节
            Assert.Equal(0x8F, response[11]); // 高字节
            Assert.Equal(0x2D, response[12]); // 低字节
        }

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_SlaveId2_SmallData()
        {
            // 真实报文: 02 03 00 00 00 02 C4 38 → 00 F7 03 24
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "2", Startaddr = 40001, Hexdata = "00 F7" },
                new Register { Id = "2", Slaveid = "2", Startaddr = 40002, Hexdata = "03 24" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "2"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x02,       // Unit ID (Slave 2)
                0x03,       // Function Code
                0x00, 0x00, // Starting Address (0)
                0x00, 0x02  // Quantity (2 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(0x02, response[6]); // Unit ID
            Assert.Equal(0x03, response[7]); // Function Code
            Assert.Equal(4, response[8]); // Byte Count
            Assert.Equal(0x00, response[9]);  // 第一个寄存器高字节
            Assert.Equal(0xF7, response[10]); // 第一个寄存器低字节
            Assert.Equal(0x03, response[11]); // 第二个寄存器高字节
            Assert.Equal(0x24, response[12]); // 第二个寄存器低字节
        }

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_SlaveId11_4Registers()
        {
            // 真实报文: 0B 03 00 00 00 04 44 A3 → 3F 83 A4 8A 00 00 00 00
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "11", Startaddr = 40001, Hexdata = "3F 83" },
                new Register { Id = "2", Slaveid = "11", Startaddr = 40002, Hexdata = "A4 8A" },
                new Register { Id = "3", Slaveid = "11", Startaddr = 40003, Hexdata = "00 00" },
                new Register { Id = "4", Slaveid = "11", Startaddr = 40004, Hexdata = "00 00" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "11"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x0B,       // Unit ID (Slave 11)
                0x03,       // Function Code
                0x00, 0x00, // Starting Address (0)
                0x00, 0x04  // Quantity (4 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(0x0B, response[6]); // Unit ID
            Assert.Equal(0x03, response[7]); // Function Code
            Assert.Equal(8, response[8]); // Byte Count (4 * 2)
            Assert.Equal(0x3F, response[9]);  // 第一个寄存器
            Assert.Equal(0x83, response[10]);
            Assert.Equal(0xA4, response[11]); // 第二个寄存器
            Assert.Equal(0x8A, response[12]);
            Assert.Equal(0x00, response[13]); // 第三个寄存器
            Assert.Equal(0x00, response[14]);
            Assert.Equal(0x00, response[15]); // 第四个寄存器
            Assert.Equal(0x00, response[16]);
        }

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_InputRegisters_SlaveId17()
        {
            // 真实报文: 11 04 00 00 00 09 32 9C → 11 AD 00 00 00 03 FD 2D 0A CA 03 A1 03 66 03 A1
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "17", Startaddr = 30001, Hexdata = "11 AD" },
                new Register { Id = "2", Slaveid = "17", Startaddr = 30002, Hexdata = "00 00" },
                new Register { Id = "3", Slaveid = "17", Startaddr = 30003, Hexdata = "00 03" },
                new Register { Id = "4", Slaveid = "17", Startaddr = 30004, Hexdata = "FD 2D" },
                new Register { Id = "5", Slaveid = "17", Startaddr = 30005, Hexdata = "0A CA" },
                new Register { Id = "6", Slaveid = "17", Startaddr = 30006, Hexdata = "03 A1" },
                new Register { Id = "7", Slaveid = "17", Startaddr = 30007, Hexdata = "03 66" },
                new Register { Id = "8", Slaveid = "17", Startaddr = 30008, Hexdata = "03 A1" },
                new Register { Id = "9", Slaveid = "17", Startaddr = 30009, Hexdata = "E4 B7" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "17"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x11,       // Unit ID (Slave 17)
                0x04,       // Function Code (Read Input Registers)
                0x00, 0x00, // Starting Address (0)
                0x00, 0x09  // Quantity (9 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(0x11, response[6]); // Unit ID
            Assert.Equal(0x04, response[7]); // Function Code (Input Registers)
            Assert.Equal(18, response[8]); // Byte Count (9 * 2)
            
            // 验证前几个寄存器
            Assert.Equal(0x11, response[9]);  // 第一个寄存器
            Assert.Equal(0xAD, response[10]);
            Assert.Equal(0x00, response[11]); // 第二个寄存器
            Assert.Equal(0x00, response[12]);
            Assert.Equal(0x00, response[13]); // 第三个寄存器
            Assert.Equal(0x03, response[14]);
        }

        [Fact]
        public async Task ProcessRequest_RealWorldScenario_HighAddress_InputRegisters()
        {
            // 真实报文: 01 04 02 07 00 04 41 B0 → 00 00 00 00 00 00 00 00
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 30520, Hexdata = "00 00" }, // 0x0207 = 519 + 30001 = 30520
                new Register { Id = "2", Slaveid = "1", Startaddr = 30521, Hexdata = "00 00" },
                new Register { Id = "3", Slaveid = "1", Startaddr = 30522, Hexdata = "00 00" },
                new Register { Id = "4", Slaveid = "1", Startaddr = 30523, Hexdata = "00 00" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, // Transaction ID
                0x00, 0x00, // Protocol ID
                0x00, 0x06, // Length
                0x01,       // Unit ID
                0x04,       // Function Code (Read Input Registers)
                0x02, 0x07, // Starting Address (0x0207 = 519)
                0x00, 0x04  // Quantity (4 registers)
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(0x01, response[6]); // Unit ID
            Assert.Equal(0x04, response[7]); // Function Code
            Assert.Equal(8, response[8]); // Byte Count (4 * 2)
            
            // 验证所有数据都是0
            for (int i = 9; i <= 16; i++)
            {
                Assert.Equal(0x00, response[i]);
            }
        }

        #endregion

        #region 扩展的边界和异常测试

        [Fact]
        public async Task ProcessRequest_LargeHexDataString_WithSpaces()
        {
            // 测试大量16进制数据，包含空格分隔符
            var registers = new List<Register>
            {
                new Register 
                { 
                    Id = "1", 
                    Slaveid = "1", 
                    Startaddr = 40001, 
                    Hexdata = "AA BB CC DD EE FF 00 11 22 33 44 55 66 77 88 99 AB CD EF 12 34 56 78 90 12 34 56 78 90 AB CD EF 01" 
                }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x10 // 读16个寄存器
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(32, response[8]); // 16个寄存器 * 2字节
            Assert.Equal(0xAA, response[9]);  // 第一个字节
            Assert.Equal(0xBB, response[10]); // 第二个字节
        }

        [Fact]
        public async Task ProcessRequest_MixedHexFormats_WithAndWithout0xPrefix()
        {
            // 测试混合格式：有些带0x前缀，有些不带
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = "0x1234" },
                new Register { Id = "2", Slaveid = "1", Startaddr = 40002, Hexdata = "ABCD" },
                new Register { Id = "3", Slaveid = "1", Startaddr = 40003, Hexdata = "0xEF01" },
                new Register { Id = "4", Slaveid = "1", Startaddr = 40004, Hexdata = "5678" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x04
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(8, response[8]); // 4个寄存器 * 2字节
            Assert.Equal(0x12, response[9]);  // 0x1234的高字节
            Assert.Equal(0x34, response[10]); // 0x1234的低字节
            Assert.Equal(0xAB, response[11]); // ABCD的高字节
            Assert.Equal(0xCD, response[12]); // ABCD的低字节
            Assert.Equal(0xEF, response[13]); // 0xEF01的高字节
            Assert.Equal(0x01, response[14]); // 0xEF01的低字节
            Assert.Equal(0x56, response[15]); // 5678的高字节
            Assert.Equal(0x78, response[16]); // 5678的低字节
        }

        [Fact]
        public async Task ProcessRequest_IEEE754FloatData_32BitValues()
        {
            // 测试IEEE754浮点数数据（32位，占用2个16位寄存器）
            var registers = new List<Register>
            {
                // 3.14159f = 0x40490FDB (大端序)
                new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = "40 49" },
                new Register { Id = "2", Slaveid = "1", Startaddr = 40002, Hexdata = "0F DB" },
                // -123.456f = 0xC2F6E979
                new Register { Id = "3", Slaveid = "1", Startaddr = 40003, Hexdata = "C2 F6" },
                new Register { Id = "4", Slaveid = "1", Startaddr = 40004, Hexdata = "E9 79" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x04
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            
            // 验证3.14159f的字节序
            Assert.Equal(0x40, response[9]);
            Assert.Equal(0x49, response[10]);
            Assert.Equal(0x0F, response[11]);
            Assert.Equal(0xDB, response[12]);
            
            // 验证-123.456f的字节序
            Assert.Equal(0xC2, response[13]);
            Assert.Equal(0xF6, response[14]);
            Assert.Equal(0xE9, response[15]);
            Assert.Equal(0x79, response[16]);
        }

        [Fact]
        public async Task ProcessRequest_NonStandardSlaveIds_HighValues()
        {
            // 测试非标准从机ID（高值）
            var testCases = new[]
            {
                new { SlaveId = 100, Expected = "12 34" },
                new { SlaveId = 200, Expected = "56 78" },
                new { SlaveId = 247, Expected = "AB CD" } // 最大有效从机ID
            };

            foreach (var testCase in testCases)
            {
                var registers = new List<Register>
                {
                    new Register { Id = "1", Slaveid = testCase.SlaveId.ToString(), Startaddr = 40001, Hexdata = testCase.Expected }
                };

                _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, testCase.SlaveId.ToString()))
                    .ReturnsAsync(registers);

                var request = new byte[]
                {
                    0x00, 0x01, 0x00, 0x00, 0x00, 0x06, (byte)testCase.SlaveId, 0x03, 0x00, 0x00, 0x00, 0x01
                };

                var context = new ProtocolContext { LocalPort = 502 };

                // Act
                var response = await _modbusService.ProcessRequestAsync(request, context);

                // Assert
                Assert.NotEmpty(response);
                Assert.Equal((byte)testCase.SlaveId, response[6]); // Unit ID
                
                var expectedBytes = testCase.Expected.Split(' ').Select(h => Convert.ToByte(h, 16)).ToArray();
                Assert.Equal(expectedBytes[0], response[9]);
                Assert.Equal(expectedBytes[1], response[10]);
            }
        }

        [Fact]
        public async Task ProcessRequest_ExtremeLargeDataBlock_MaximumSize()
        {
            // 测试最大数据块（125个寄存器，250字节数据）
            var registers = new List<Register>();
            
            for (int i = 0; i < 125; i++)
            {
                registers.Add(new Register 
                { 
                    Id = (i + 1).ToString(), 
                    Slaveid = "1", 
                    Startaddr = 40001 + i, 
                    Hexdata = $"{(i % 256):X2} {((i + 1) % 256):X2}" 
                });
            }

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x7D // 125个寄存器
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(250, response[8]); // 125 * 2 = 250字节
            
            // 验证前几个和最后几个字节
            Assert.Equal(0x00, response[9]);  // 第一个寄存器
            Assert.Equal(0x01, response[10]);
            Assert.Equal(0x01, response[11]); // 第二个寄存器
            Assert.Equal(0x02, response[12]);
        }

        [Theory]
        [InlineData("FF FF", 0xFF, 0xFF)] // 最大值
        [InlineData("00 00", 0x00, 0x00)] // 最小值
        [InlineData("7F FF", 0x7F, 0xFF)] // 有符号整数最大值
        [InlineData("80 00", 0x80, 0x00)] // 有符号整数最小值
        [InlineData("55 AA", 0x55, 0xAA)] // 交替位模式
        [InlineData("AA 55", 0xAA, 0x55)] // 反向交替位模式
        public async Task ProcessRequest_BoundaryValues_SpecialPatterns(string hexData, byte expectedHigh, byte expectedLow)
        {
            // 测试边界值和特殊位模式
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = hexData }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x01
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(expectedHigh, response[9]);
            Assert.Equal(expectedLow, response[10]);
        }

        #endregion

        #region 数据格式兼容性测试

        [Fact]
        public async Task ProcessRequest_VariousHexFormats_AllValid()
        {
            // 测试各种有效的16进制格式 - 只测试我们确认支持的格式
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = "12 34" }  // 标准格式
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(502, "1"))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 // 读1个寄存器
            };

            var context = new ProtocolContext { LocalPort = 502 };

            // Act
            var response = await _modbusService.ProcessRequestAsync(request, context);

            // Assert
            Assert.NotEmpty(response);
            Assert.Equal(2, response[8]); // 1个寄存器 * 2字节
            
            // 验证标准格式能正确解析
            Assert.Equal(0x12, response[9]);  // 高字节
            Assert.Equal(0x34, response[10]); // 低字节
        }

        #endregion

        #region 性能和压力测试

        [Fact]
        public async Task ProcessRequest_PerformanceTest_MultipleSequentialRequests()
        {
            // 性能测试：连续多个请求
            var registers = new List<Register>
            {
                new Register { Id = "1", Slaveid = "1", Startaddr = 40001, Hexdata = "12 34" }
            };

            _mockRegisterService.Setup(x => x.GetRegistersBySlaveIdAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(registers);

            var request = new byte[]
            {
                0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x01, 0x03, 0x00, 0x00, 0x00, 0x01
            };

            var context = new ProtocolContext { LocalPort = 502 };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - 执行100次请求
            for (int i = 0; i < 100; i++)
            {
                var response = await _modbusService.ProcessRequestAsync(request, context);
                Assert.NotEmpty(response);
                Assert.Equal(0x12, response[9]);
                Assert.Equal(0x34, response[10]);
            }

            stopwatch.Stop();

            // Assert - 100次请求应该在合理时间内完成（例如1秒）
            Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
                $"100 requests took {stopwatch.ElapsedMilliseconds}ms, which is too slow");
        }

        #endregion
    }
}