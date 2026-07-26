using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.KitchenOrders;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KitchenOrderTicketsController : ControllerBase
{
    private readonly IKitchenOrderTicketService _service;

    public KitchenOrderTicketsController(IKitchenOrderTicketService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateKitchenOrderTicketDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }
}