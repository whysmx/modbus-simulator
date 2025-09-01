using ModbusSimulator.Models;

namespace ModbusSimulator.Services;

public interface IRegisterService
{
    Task<IEnumerable<Register>> GetRegistersBySlaveIdAsync(string slaveId);
    Task<Register> CreateRegisterAsync(string connectionId, string slaveId, CreateRegisterRequest request);
    Task<Register> UpdateRegisterAsync(string connectionId, string slaveId, string registerId, UpdateRegisterRequest request);
    Task DeleteRegisterAsync(string connectionId, string slaveId, string registerId);
}
