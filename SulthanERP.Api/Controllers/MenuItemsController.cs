using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sulthan.Core.Entities;
using Sulthan.Core.Interfaces;

namespace SulthanERP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MenuItemsController : ControllerBase
    {
        private readonly IMenuItemRepository _repository;

        public MenuItemsController(IMenuItemRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _repository.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
                return NotFound();

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(MenuItem menuItem)
        {
            var existing = await _repository.GetByNameAsync(menuItem.Name);

            if (existing != null)
                return BadRequest("Menu item already exists.");

            await _repository.AddAsync(menuItem);

            return Ok(menuItem);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, MenuItem model)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
                return NotFound();

            item.Name = model.Name;
            item.TamilName = model.TamilName;
            item.CategoryId = model.CategoryId;
            item.ACPrice = model.ACPrice;
            item.NonACPrice = model.NonACPrice;
            item.ParcelPrice = model.ParcelPrice;
            item.KitchenName = model.KitchenName;
            item.IsAvailable = model.IsAvailable;
            item.IsParcelAvailable = model.IsParcelAvailable;
            item.DisplayOrder = model.DisplayOrder;
            item.IsActive = model.IsActive;

            await _repository.UpdateAsync(item);

            return Ok(item);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetByIdAsync(id);

            if (item == null)
                return NotFound();

            await _repository.DeleteAsync(item);

            return Ok("Menu item deleted successfully.");
        }
    }
}