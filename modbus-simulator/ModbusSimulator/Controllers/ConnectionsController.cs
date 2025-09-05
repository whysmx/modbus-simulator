using Microsoft.AspNetCore.Mvc;
using ModbusSimulator.Models;
using ModbusSimulator.Services;
using ModbusSimulator.Enums;

namespace ModbusSimulator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionService _connectionService;

    public ConnectionsController(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }
    
    [HttpPost]
    public async Task<ActionResult<Connection>> CreateConnection([FromBody] CreateConnectionRequest request)
    {
        try
        {
            var created = await _connectionService.CreateConnectionAsync(request);
            return CreatedAtAction(nameof(CreateConnection), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message, code = 400 });
        }
    }
    
    [HttpPut("{id}")]
    public async Task<ActionResult<Connection>> UpdateConnection(string id, [FromBody] UpdateConnectionRequest request)
    {
        try
        {
            var updated = await _connectionService.UpdateConnectionAsync(id, request);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteConnection(string id)
    {
        try
        {
            await _connectionService.DeleteConnectionAsync(id);
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

    [HttpGet("tree")]
    public async Task<ActionResult<IEnumerable<ConnectionTree>>> GetConnectionsTree()
    {
        var connectionTrees = await _connectionService.GetConnectionsTreeAsync();
        return Ok(connectionTrees);
    }

    [HttpGet("protocol-types")]
    public ActionResult GetProtocolTypes()
    {
        var protocolTypes = Enum.GetValues<ModbusProtocolType>()
            .Select(pt => new 
            {
                value = (int)pt,
                name = pt.ToString(),
                displayName = pt.GetDisplayName(),
                description = pt.GetDescription()
            })
            .ToArray();
        
        return Ok(protocolTypes);
    }
}