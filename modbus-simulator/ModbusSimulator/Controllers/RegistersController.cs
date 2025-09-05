using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Models;
using ModbusSimulator.Services;

namespace ModbusSimulator.Controllers;

[ApiController]
[Route("api/connections/{connectionId}/slaves/{slaveId}/registers")]
public class RegistersController : ControllerBase
{
    private readonly IRegisterService _registerService;
    private readonly IConnectionService _connectionService;
    
    public RegistersController(IRegisterService registerService, IConnectionService connectionService)
    {
        _registerService = registerService;
        _connectionService = connectionService;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Register>>> GetRegisters(string connectionId, string slaveId)
    {
        try
        {
            // 获取连接信息以获取端口
            var connection = await _connectionService.GetConnectionByIdAsync(connectionId);
            var registers = await _registerService.GetRegistersBySlaveIdAsync(connection.Port, slaveId);
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
        if (request == null)
        {
            return BadRequest(new { error = "请求不能为空" });
        }
        
        try
        {
            var created = await _registerService.CreateRegisterAsync(connectionId, slaveId, request);
            return CreatedAtAction(nameof(GetRegisters), new { connectionId, slaveId }, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpPut("{registerId}")]
    public async Task<ActionResult<Register>> UpdateRegister(string connectionId, string slaveId, string registerId, [FromBody] UpdateRegisterRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "请求不能为空" });
        }
        
        try
        {
            var updated = await _registerService.UpdateRegisterAsync(connectionId, slaveId, registerId, request);
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpDelete("{registerId}")]
    public async Task<IActionResult> DeleteRegister(string connectionId, string slaveId, string registerId)
    {
        try
        {
            await _registerService.DeleteRegisterAsync(connectionId, slaveId, registerId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
}