using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.DTOs.Tables;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiningTablesController : ControllerBase
{
	private readonly ITableService _tableService;

	public DiningTablesController(ITableService tableService)
	{
		_tableService = tableService;
	}

	[HttpGet]
	public async Task<IActionResult> GetAll()
	{
		var result = await _tableService.GetAllAsync();
		return Ok(result);
	}

	[HttpGet("{id:int}")]
	public async Task<IActionResult> GetById(int id)
	{
		var result = await _tableService.GetByIdAsync(id);

		if (result == null)
			return NotFound("Table not found.");

		return Ok(result);
	}

	[HttpPost]
	public async Task<IActionResult> Create(CreateDiningTableDto dto)
	{
		var result = await _tableService.CreateAsync(dto);

		return CreatedAtAction(
			nameof(GetById),
			new { id = result.Id },
			result);
	}

	[HttpPut("{id:int}")]
	public async Task<IActionResult> Update(int id, UpdateDiningTableDto dto)
	{
		var result = await _tableService.UpdateAsync(id, dto);

		return Ok(result);
	}

	[HttpDelete("{id:int}")]
	public async Task<IActionResult> Delete(int id)
	{
		await _tableService.DeleteAsync(id);

		return Ok("Table deleted successfully.");
	}
}