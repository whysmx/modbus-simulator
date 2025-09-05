namespace ModbusSimulator.Enums;

public enum ModbusProtocolType
{
    /// <summary>
    /// Modbus RTU over TCP - 传统 RTU 帧通过 TCP 传输（包含CRC校验）
    /// </summary>
    ModbusRtuOverTcp = 0,
    
    /// <summary>
    /// Modbus TCP - 标准 TCP 协议（包含MBAP头）
    /// </summary>
    ModbusTcp = 1
}

public static class ModbusProtocolTypeExtensions
{
    public static string GetDisplayName(this ModbusProtocolType protocolType)
    {
        return protocolType switch
        {
            ModbusProtocolType.ModbusRtuOverTcp => "Modbus RTU over TCP",
            ModbusProtocolType.ModbusTcp => "Modbus TCP",
            _ => throw new ArgumentOutOfRangeException(nameof(protocolType))
        };
    }

    public static string GetDescription(this ModbusProtocolType protocolType)
    {
        return protocolType switch
        {
            ModbusProtocolType.ModbusRtuOverTcp => "传统 Modbus RTU 协议帧通过 TCP 连接传输，保留 CRC 校验",
            ModbusProtocolType.ModbusTcp => "标准 Modbus TCP 协议，使用 MBAP 头进行帧封装",
            _ => throw new ArgumentOutOfRangeException(nameof(protocolType))
        };
    }
}