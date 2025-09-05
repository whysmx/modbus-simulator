using ModbusSimulator.Enums;
using ModbusSimulator.Services;

namespace ModbusSimulator.Tcp;

public interface IProtocolHandlerFactory
{
    IProtocolHandler CreateHandler(ModbusProtocolType protocolType);
}

public class ProtocolHandlerFactory : IProtocolHandlerFactory
{
    private readonly IRegisterService _registerService;

    public ProtocolHandlerFactory(IRegisterService registerService)
    {
        _registerService = registerService;
    }

    public IProtocolHandler CreateHandler(ModbusProtocolType protocolType)
    {
        return protocolType switch
        {
            ModbusProtocolType.ModbusRtuOverTcp => new ModbusRtuOverTcpService(_registerService),
            ModbusProtocolType.ModbusTcp => new ModbusTcpService(_registerService),
            _ => throw new ArgumentException($"Unsupported protocol type: {protocolType}")
        };
    }
}