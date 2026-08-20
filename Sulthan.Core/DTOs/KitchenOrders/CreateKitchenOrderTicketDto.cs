using System.ComponentModel.DataAnnotations;

namespace Sulthan.Core.DTOs.KitchenOrders;

public class CreateKitchenOrderTicketDto
{
    [Required]
    public int OrderId { get; set; }

    [MinLength(1, ErrorMessage = "At least one item required.")]
    public List<CreateKitchenOrderTicketItemDto> Items { get; set; } = new();
}