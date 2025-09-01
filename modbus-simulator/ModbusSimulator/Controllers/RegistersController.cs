using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Controllers;

[ApiController]
[Route("api/connections/{connectionId}/slaves/{slaveId}/registers")]
public class RegistersController : ControllerBase
{
    private readonly IRegisterRepository _registerRepository;
    
    public RegistersController(IRegisterRepository registerRepository)
    {
        _registerRepository = registerRepository;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Register>>> GetRegisters(string connectionId, string slaveId)
    {
        try
        {
            var registers = await _registerRepository.GetBySlaveIdAsync(slaveId);
            return Ok(registers);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
    }
    
    [HttpPost]
    public async Task<ActionResult<Register>> CreateRegister(string connectionId, string slaveId, [FromBody] CreateRegisterRequest request)
    {
        try
        {
            var register = new Register 
            { 
                Slaveid = slaveId, 
                Startaddr = request.Startaddr, 
                Hexdata = request.Hexdata 
            };
            var created = await _registerRepository.CreateAsync(register);
            return StatusCode(201, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpPut("{registerId}")]
    public async Task<ActionResult<Register>> UpdateRegister(string connectionId, string slaveId, string registerId, [FromBody] UpdateRegisterRequest request)
    {
        try
        {
            var register = new Register 
            { 
                Id = registerId, 
                Slaveid = slaveId, 
                Startaddr = request.Startaddr, 
                Hexdata = request.Hexdata 
            };
            var updated = await _registerRepository.UpdateAsync(register);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpDelete("{registerId}")]
    public async Task<IActionResult> DeleteRegister(string connectionId, string slaveId, string registerId)
    {
        try
        {
            await _registerRepository.DeleteAsync(registerId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
    }
}