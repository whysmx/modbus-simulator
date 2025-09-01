using ModbusSimulator.Models;

namespace ModbusSimulator.Services;

public interface ISlaveService
{
    Task<Slave> CreateSlaveAsync(string connectionId, CreateSlaveRequest request);
    Task<Slave> UpdateSlaveAsync(string connectionId, string slaveId, UpdateSlaveRequest request);
    Task DeleteSlaveAsync(string connectionId, string slaveId);
}
