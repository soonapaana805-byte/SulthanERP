using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.Orders;
using Sulthan.Core.DTOs.Orders.Response;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllAsync();

        var result = orders.Select(order => new OrderDetailsResponseDto
        {
            Id = order.Id,
            BillNumber = order.BillNumber,
            OrderType = order.OrderType.ToString(),
            BillStatus = order.BillStatus.ToString(),

            Table = order.DiningTable == null
                ? null
                : new TableSummaryDto
                {
                    Id = order.DiningTable.Id,
                    TableNumber = order.DiningTable.TableNumber
                },

            Captain = order.User == null
                ? null
                : new UserSummaryDto
                {
                    Id = order.User.Id,
                    FullName = order.User.FullName
                },

            SubTotal = order.SubTotal,
            Discount = order.Discount,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,
            Remarks = order.Remarks,

            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                MenuItemId = i.MenuItemId,
                ItemName = i.MenuItem?.Name ?? "",
                Price = i.Price,
                Quantity = i.Quantity,
                Notes = i.Notes
            }).ToList()
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);

        if (order == null)
            return NotFound();

        var result = new OrderDetailsResponseDto
        {
            Id = order.Id,
            BillNumber = order.BillNumber,
            OrderType = order.OrderType.ToString(),
            BillStatus = order.BillStatus.ToString(),

            Table = order.DiningTable == null
                ? null
                : new TableSummaryDto
                {
                    Id = order.DiningTable.Id,
                    TableNumber = order.DiningTable.TableNumber
                },

            Captain = order.User == null
                ? null
                : new UserSummaryDto
                {
                    Id = order.User.Id,
                    FullName = order.User.FullName
                },

            SubTotal = order.SubTotal,
            Discount = order.Discount,
            Tax = order.Tax,
            GrandTotal = order.GrandTotal,
            Remarks = order.Remarks,

            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                MenuItemId = i.MenuItemId,
                ItemName = i.MenuItem?.Name ?? "",
                Price = i.Price,
                Quantity = i.Quantity,
                Notes = i.Notes
            }).ToList()
        };

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        var order = await _orderService.AddAsync(dto);
        return Ok(order);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateOrderDto dto)
    {
        var order = await _orderService.UpdateAsync(id, dto);
        return Ok(order);
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id, CompleteOrderDto dto)
    {
        var order = await _orderService.CompleteOrderAsync(id, dto);
        return Ok(order);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _orderService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}