using System.Collections.Generic;

namespace ProductSearchService.API.Events
{
    public class ProductsCreatedEvent
    {
        public List<CreateProductsItem> Products { get; set; } = new List<CreateProductsItem>();
    }

    public class CreateProductsItem
    {
        public string Productnumber { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }
}
