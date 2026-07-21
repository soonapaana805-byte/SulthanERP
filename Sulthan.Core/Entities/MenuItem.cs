using System.Text.Json.Serialization;
using Sulthan.Core.Common;

namespace Sulthan.Core.Entities
{
    public class MenuItem : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? TamilName { get; set; }

        public int CategoryId { get; set; }

        public decimal ACPrice { get; set; }

        public decimal NonACPrice { get; set; }

        public decimal ParcelPrice { get; set; }

        public string KitchenName { get; set; } = "Main Kitchen";

        public bool IsAvailable { get; set; } = true;

        public bool IsParcelAvailable { get; set; } = true;

        public int DisplayOrder { get; set; }

        [JsonIgnore]
        public Category? Category { get; set; }
    }
}