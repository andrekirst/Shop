using System.ComponentModel.DataAnnotations;

namespace ProductSearchService.API.Model
{
    public class Product
    {
        public string ProductId { get; set; }

        [StringLength(maximumLength: 256)]
        public string Productnumber { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
