using ModbusSimulator.Models;

namespace ModbusSimulator.Repositories;

public interface ISlaveRepository
{
    Task<Slave> CreateAsync(Slave slave);
    Task<Slave> UpdateAsync(Slave slave);
    Task DeleteAsync(string id);
}