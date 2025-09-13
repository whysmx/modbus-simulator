namespace ModbusSimulator.Models;

public class Register
{
    public string Id { get; set; } = string.Empty;
    public string Slaveid { get; set; } = string.Empty;
    public int Startaddr { get; set; }
    public string Hexdata { get; set; } = string.Empty;
    public string Names { get; set; } = string.Empty;
    public string Coefficients { get; set; } = string.Empty;
}

public class CreateRegisterRequest
{
    public int Startaddr { get; set; }
    public string Hexdata { get; set; } = string.Empty;
    public string Names { get; set; } = string.Empty;
    public string Coefficients { get; set; } = string.Empty;
}

public class UpdateRegisterRequest
{
    public int Startaddr { get; set; }
    public string Hexdata { get; set; } = string.Empty;
    public string Names { get; set; } = string.Empty;
    public string Coefficients { get; set; } = string.Empty;
}