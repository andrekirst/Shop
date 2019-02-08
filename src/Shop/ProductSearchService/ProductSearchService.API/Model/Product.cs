using System.ComponentModel.DataAnnotations.Schema;

namespace ProductSearchService.API.Model
{
    [Table(name: "Products")]
    public class Product
    {
        public string Productnumber { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
