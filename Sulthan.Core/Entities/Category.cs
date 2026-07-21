using Sulthan.Core.Common;

namespace Sulthan.Core.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        // Navigation Property
        public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}