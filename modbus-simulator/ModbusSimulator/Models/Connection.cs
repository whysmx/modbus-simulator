using ModbusSimulator.Enums;

namespace ModbusSimulator.Models;

public class Connection
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public ModbusProtocolType ProtocolType { get; set; } = ModbusProtocolType.ModbusRtuOverTcp;
}

public class ConnectionTree : Connection
{
    public List<Slave> Slaves { get; set; } = new();
}

public class CreateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public ModbusProtocolType ProtocolType { get; set; } = ModbusProtocolType.ModbusRtuOverTcp;
}

public class UpdateConnectionRequest
{
    public string Name { get; set; } = string.Empty;
    public int Port { get; set; }
    public ModbusProtocolType ProtocolType { get; set; } = ModbusProtocolType.ModbusRtuOverTcp;
}