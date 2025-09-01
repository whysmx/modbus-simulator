namespace ModbusSimulator.Models;

public class Slave
{
    public string Id { get; set; } = string.Empty;
    public string Connid { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Slaveid { get; set; }
}

public class CreateSlaveRequest
{
    public string Name { get; set; } = string.Empty;
    public int Slaveid { get; set; }
}

public class UpdateSlaveRequest
{
    public string Name { get; set; } = string.Empty;
    public int Slaveid { get; set; }
}