using System.ComponentModel.DataAnnotations;

namespace ProductSearchService.EventListener.Model
{
    public class Product
    {
        [StringLength(maximumLength: 256)]
        public string Productnumber { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
