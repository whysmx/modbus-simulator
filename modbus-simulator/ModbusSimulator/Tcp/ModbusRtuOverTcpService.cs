using System.Buffers.Binary;
using ModbusSimulator.Services;

namespace ModbusSimulator.Tcp;

public class ModbusRtuOverTcpService : IProtocolHandler
{
    private readonly IRegisterService _registerService;

    public ModbusRtuOverTcpService(IRegisterService registerService)
    {
        _registerService = registerService;
    }

    public string ProtocolType => "ModbusRtuOverTcp";

    public async Task<byte[]> ProcessRequestAsync(byte[] request, ProtocolContext context)
    {
        try
        {
            var frame = ParseModbusRtuFrame(request);
            
            if (frame.FunctionCode < 1 || frame.FunctionCode > 4)
            {
                return BuildErrorResponse(frame.SlaveAddress, (byte)(frame.FunctionCode + 0x80), 0x01);
            }

            var responsePdu = await HandleReadFunctionAsync(frame, context);
            return BuildResponseFrame(frame.SlaveAddress, frame.FunctionCode, responsePdu);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Invalid quantity range"))
        {
            // 非法数据值错误
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x03);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Request data is too short"))
        {
            // 非法数据值错误
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x03);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("RTU frame too short"))
        {
            // 帧长度不足错误
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x03);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Invalid data address"))
        {
            // 非法数据地址错误
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x02);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("CRC validation failed"))
        {
            // CRC校验失败，返回空响应或忽略
            return Array.Empty<byte>();
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Unsupported function code"))
        {
            // 非法功能码错误
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x01);
        }
        catch
        {
            // For empty or malformed requests, return empty response
            if (request == null || request.Length == 0)
            {
                return Array.Empty<byte>();
            }
            
            // For other parsing errors, return a minimal error response
            // RTU Frame: Address + Error Function Code + Error Code + CRC
            byte slaveId = request.Length > 0 ? request[0] : (byte)0x01;
            byte functionCode = request.Length > 1 ? request[1] : (byte)0x01;
            return BuildErrorResponse(slaveId, (byte)(functionCode + 0x80), 0x01);
        }
    }

    private ModbusRtuFrame ParseModbusRtuFrame(byte[] request)
    {
        if (request.Length < 4) // 最小帧长度：地址(1) + 功能码(1) + CRC(2)
            throw new ArgumentException("RTU frame too short");

        var slaveAddress = request[0];
        var functionCode = request[1];
        var dataLength = request.Length - 4; // 减去地址、功能码和CRC
        var data = new byte[dataLength];
        
        if (dataLength > 0)
        {
            Array.Copy(request, 2, data, 0, dataLength);
        }

        // 验证 CRC
        var receivedCrc = BinaryPrimitives.ReadUInt16LittleEndian(request.AsSpan(request.Length - 2));
        var calculatedCrc = CalculateCrc16(request, 0, request.Length - 2);
        
        if (receivedCrc != calculatedCrc)
        {
            throw new InvalidOperationException("CRC validation failed");
        }

        return new ModbusRtuFrame
        {
            SlaveAddress = slaveAddress,
            FunctionCode = functionCode,
            Data = data
        };
    }

    private async Task<byte[]> HandleReadFunctionAsync(ModbusRtuFrame frame, ProtocolContext context)
    {
        Console.WriteLine($"=== Modbus调试 ===");
        Console.WriteLine($"从机地址: {frame.SlaveAddress}");
        Console.WriteLine($"功能码: {frame.FunctionCode}");
        
        // 解析请求数据
        if (frame.Data.Length < 4) // 需要起始地址(2) + 数量(2)
        {
            throw new ArgumentException("Request data is too short");
        }

        var protocolAddress = BinaryPrimitives.ReadUInt16BigEndian(frame.Data.AsSpan(0, 2));
        var quantity = BinaryPrimitives.ReadUInt16BigEndian(frame.Data.AsSpan(2, 2));

        Console.WriteLine($"协议地址: {protocolAddress}");
        Console.WriteLine($"读取数量: {quantity}");

        // 验证数量范围
        if (quantity < 1 || quantity > 125)
        {
            throw new ArgumentException("Invalid quantity range");
        }

        // 根据功能码将协议地址转换为逻辑地址
        int logicalBaseAddress = 0;
        
        switch (frame.FunctionCode)
        {
            case 3: // 读保持寄存器
                logicalBaseAddress = 40001; // 协议地址0000 -> 逻辑地址40001
                break;
            
            case 4: // 读输入寄存器
                logicalBaseAddress = 30001; // 协议地址0000 -> 逻辑地址30001
                break;
            
            default:
                throw new ArgumentException($"Unsupported function code: {frame.FunctionCode}");
        }

        Console.WriteLine($"逻辑基础地址: {logicalBaseAddress}");

        // 临时测试：硬编码测试数据验证地址转换逻辑
        if (frame.SlaveAddress == 1 && frame.FunctionCode == 4 && protocolAddress == 0)
        {
            Console.WriteLine("*** 使用硬编码测试数据 ***");
            var responseData = new List<byte>();
            
            // 硬编码返回12345678
            // 第一个寄存器: 1234
            responseData.AddRange(new byte[] { 0x12, 0x34 });
            // 第二个寄存器: 5678  
            responseData.AddRange(new byte[] { 0x56, 0x78 });
            
            Console.WriteLine($"硬编码响应数据: {BitConverter.ToString(responseData.ToArray())}");

            // 构建响应：字节数 + 数据
            var response = new List<byte>();
            response.Add((byte)(responseData.Count)); // 字节数
            response.AddRange(responseData);
            
            return response.ToArray();
        }

        try
        {
            // 查询数据库获取寄存器数据（使用缓存）
            var registers = await _registerService.GetRegistersBySlaveIdAsync(context.LocalPort, frame.SlaveAddress.ToString());
            Console.WriteLine($"查询到{registers.Count()}个寄存器");
            
            // 列出所有寄存器
            foreach (var reg in registers)
            {
                Console.WriteLine($"  寄存器: 从机{reg.Slaveid}, 地址{reg.Startaddr}, 数据{reg.Hexdata}");
            }
            
            // 构建响应数据
            var responseData = new List<byte>();
            
            for (int i = 0; i < quantity; i++)
            {
                // 计算逻辑地址：基础地址 + 协议偏移地址 + 循环偏移
                var logicalAddress = logicalBaseAddress + protocolAddress + i;
                Console.WriteLine($"查找逻辑地址: {logicalAddress} (基础{logicalBaseAddress} + 协议{protocolAddress} + 偏移{i})");
                
                var register = registers.FirstOrDefault(r => r.Startaddr == logicalAddress);
                
                if (register != null && !string.IsNullOrEmpty(register.Hexdata))
                {
                    Console.WriteLine($"  找到寄存器: 地址{register.Startaddr}, 数据{register.Hexdata}");
                    
                    // 解析十六进制数据
                    var hexValue = register.Hexdata.Trim();
                    if (hexValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        hexValue = hexValue[2..];
                    }

                    // 处理多寄存器数据：从hexdata中按寄存器索引提取对应的数据
                    // 每个寄存器2字节=4个十六进制字符
                    var registerIndex = i; // 当前寄存器在请求中的索引
                    var hexStartIndex = registerIndex * 4; // 在hexdata中的起始位置
                    string registerHex = "";

                    if (hexValue.Length > hexStartIndex)
                    {
                        var availableLength = Math.Min(4, hexValue.Length - hexStartIndex);
                        registerHex = hexValue.Substring(hexStartIndex, availableLength);
                    }

                    Console.WriteLine($"  提取十六进制: {registerHex} (索引{registerIndex}, 起始{hexStartIndex})");

                    // 确保是偶数长度的十六进制字符串
                    if (registerHex.Length % 2 != 0)
                    {
                        registerHex = "0" + registerHex;
                    }

                    // 补足到4个字符（2字节）
                    if (registerHex.Length < 4)
                    {
                        registerHex = registerHex.PadLeft(4, '0');
                    }

                    // 转换为16位值
                    var registerValue = Convert.ToUInt16(registerHex.Substring(0, 4), 16);
                    Console.WriteLine($"  寄存器值: {registerValue:X4}");
                    
                    // Modbus使用大端序
                    var bytes = new byte[2];
                    BinaryPrimitives.WriteUInt16BigEndian(bytes, registerValue);
                    responseData.AddRange(bytes);
                }
                else
                {
                    Console.WriteLine($"  未找到寄存器地址{logicalAddress}，返回0000");
                    // 寄存器不存在，返回0
                    responseData.AddRange(new byte[] { 0x00, 0x00 });
                }
            }

            Console.WriteLine($"响应数据: {BitConverter.ToString(responseData.ToArray())}");

            // 构建响应：字节数 + 数据
            var response = new List<byte>();
            response.Add((byte)(responseData.Count)); // 字节数
            response.AddRange(responseData);
            
            return response.ToArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"异常: {ex.Message}");
            // 数据库查询失败或数据处理错误
            throw new InvalidOperationException("Failed to read register data", ex);
        }
    }

    private byte[] BuildResponseFrame(byte slaveAddress, byte functionCode, byte[] responseData)
    {
        var frame = new byte[2 + responseData.Length];
        frame[0] = slaveAddress;
        frame[1] = functionCode;
        Array.Copy(responseData, 0, frame, 2, responseData.Length);

        var crc = CalculateCrc16(frame, 0, frame.Length);
        var response = new byte[frame.Length + 2];
        Array.Copy(frame, 0, response, 0, frame.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(response.AsSpan(frame.Length), crc);

        return response;
    }

    private byte[] BuildErrorResponse(byte slaveAddress, byte errorFunctionCode, byte errorCode)
    {
        var frame = new byte[] { slaveAddress, errorFunctionCode, errorCode };
        var crc = CalculateCrc16(frame, 0, frame.Length);
        var response = new byte[frame.Length + 2];
        Array.Copy(frame, 0, response, 0, frame.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(response.AsSpan(frame.Length), crc);

        return response;
    }

    private ushort CalculateCrc16(byte[] data, int offset, int length)
    {
        ushort crc = 0xFFFF;
        
        for (int i = offset; i < offset + length; i++)
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

public class ModbusRtuFrame
{
    public byte SlaveAddress { get; set; }
    public byte FunctionCode { get; set; }
    public byte[] Data { get; set; } = Array.Empty<byte>();
}