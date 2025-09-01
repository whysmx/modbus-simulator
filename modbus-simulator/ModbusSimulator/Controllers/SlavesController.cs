using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Models;
using ModbusSimulator.Repositories;

namespace ModbusSimulator.Controllers;

[ApiController]
[Route("api/connections/{connectionId}/slaves")]
public class SlavesController : ControllerBase
{
    private readonly ISlaveRepository _slaveRepository;
    
    public SlavesController(ISlaveRepository slaveRepository)
    {
        _slaveRepository = slaveRepository;
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
            var slave = new Slave 
            { 
                Connid = connectionId, 
                Name = request.Name, 
                Slaveid = request.Slaveid 
            };
            var created = await _slaveRepository.CreateAsync(slave);
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
    
    [HttpPut("{slaveId}")]
    public async Task<ActionResult<Slave>> UpdateSlave(string connectionId, string slaveId, [FromBody] UpdateSlaveRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { error = "请求不能为空" });
        }
        
        try
        {
            var slave = new Slave 
            { 
                Id = slaveId, 
                Connid = connectionId, 
                Name = request.Name, 
                Slaveid = request.Slaveid 
            };
            var updated = await _slaveRepository.UpdateAsync(slave);
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
            await _slaveRepository.DeleteAsync(slaveId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message, code = 404 });
        }
    }
}