using Hotel.Api.DTOs;
using Hotel.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Hotel.Api.Controllers;

[ApiController]
[Route("api/order")]
[Authorize(Policy = "CustomerOnly")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromBody] AddOrderItemDto dto, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerGlobalId))
            return Unauthorized(new { error = "Invalid customer session" });

        try
        {
            var result = await _orderService.AddAsync(customerGlobalId, dto, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerGlobalId))
            return Unauthorized(new { error = "Invalid customer session" });

        try
        {
            var result = await _orderService.GetCurrentAsync(customerGlobalId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("item/{orderItemId:guid}")]
    public async Task<IActionResult> DeleteItem(Guid orderItemId, CancellationToken cancellationToken)
    {
        if (!TryGetCustomerId(out var customerGlobalId))
            return Unauthorized(new { error = "Invalid customer session" });

        try
        {
            var result = await _orderService.DeleteItemAsync(customerGlobalId, orderItemId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private bool TryGetCustomerId(out Guid customerGlobalId)
    {
        var value = User.FindFirstValue("customer_id");
        return Guid.TryParse(value, out customerGlobalId);
    }
}
