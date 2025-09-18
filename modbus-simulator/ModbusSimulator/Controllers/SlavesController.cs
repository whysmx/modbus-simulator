using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Models;
using ModbusSimulator.Services;

namespace ModbusSimulator.Controllers;

[ApiController]
[Route("api/connections/{connectionId}/slaves")]
public class SlavesController : ControllerBase
{
    private readonly ISlaveService _slaveService;
    
    public SlavesController(ISlaveService slaveService)
    {
        _slaveService = slaveService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Slave>> CreateSlave(string connectionId, [FromBody] CreateSlaveRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "请求不能为空" });
        }
        
        try
        {
            var created = await _slaveService.CreateSlaveAsync(connectionId, request);
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpPut("{slaveId}")]
    public async Task<ActionResult<Slave>> UpdateSlave(string connectionId, string slaveId, [FromBody] UpdateSlaveRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "请求不能为空" });
        }
        
        try
        {
            var updated = await _slaveService.UpdateSlaveAsync(connectionId, slaveId, request);
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
    
    [HttpDelete("{slaveId}")]
    public async Task<IActionResult> DeleteSlave(string connectionId, string slaveId)
    {
        try
        {
            await _slaveService.DeleteSlaveAsync(connectionId, slaveId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
    }
}