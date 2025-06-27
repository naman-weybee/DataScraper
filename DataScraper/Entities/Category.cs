using System.ComponentModel.DataAnnotations;

namespace DataScraper.Entities
{
    public class Category : Base
    {
        public Guid Id { get; set; }

        public Guid? ParentCategoryId { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }
    }
}