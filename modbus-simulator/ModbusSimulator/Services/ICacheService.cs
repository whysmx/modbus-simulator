namespace ModbusSimulator.Services;

public interface ICacheService
{
    void ClearSlaveCache(int port, int slaveid);
    void ClearConnectionCache(string connectionId);
}