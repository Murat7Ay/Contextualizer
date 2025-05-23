using Microsoft.AspNetCore.Mvc;
using Contextualizer.Studio.Api.Models;
using Contextualizer.Studio.Api.Services;

namespace Contextualizer.Studio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HandlersController : ControllerBase
{
    private readonly IHandlerService _handlerService;
    private readonly ILogger<HandlersController> _logger;

    public HandlersController(IHandlerService handlerService, ILogger<HandlersController> logger)
    {
        _handlerService = handlerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Handler>>> GetHandlers()
    {
        try
        {
            var handlers = await _handlerService.GetHandlersAsync();
            return Ok(handlers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting handlers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Handler>> GetHandler(string id)
    {
        try
        {
            var handler = await _handlerService.GetHandlerAsync(id);
            if (handler == null)
            {
                return NotFound();
            }
            return Ok(handler);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting handler {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<Handler>> CreateHandler(Handler handler)
    {
        try
        {
            var createdHandler = await _handlerService.CreateHandlerAsync(handler);
            return CreatedAtAction(nameof(GetHandler), new { id = createdHandler.Id }, createdHandler);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating handler");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Handler>> UpdateHandler(string id, Handler handler)
    {
        try
        {
            var updatedHandler = await _handlerService.UpdateHandlerAsync(id, handler);
            return Ok(updatedHandler);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating handler {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHandler(string id)
    {
        try
        {
            await _handlerService.DeleteHandlerAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting handler {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<Handler>> UploadHandler(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var handler = await _handlerService.UploadHandlerAsync(stream);
            return CreatedAtAction(nameof(GetHandler), new { id = handler.Id }, handler);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading handler");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/install")]
    public async Task<IActionResult> InstallHandler(string id)
    {
        try
        {
            await _handlerService.InstallHandlerAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing handler {Id}", id);
            return StatusCode(500, "Internal server error");
        }
    }
} 