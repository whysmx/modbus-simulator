using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public interface IRegisterRepository
{
    Task<IEnumerable<Register>> GetBySlaveIdAsync(string slaveId);
    Task<Register> CreateAsync(Register register);
    Task<Register> UpdateAsync(Register register);
    Task<bool> DeleteAsync(string id);
}