using System.Collections.Generic;

namespace ProductSearchService.API.Events
{
    public class ProductsCreatedEvent
    {
        public List<CreateProductsItem> Products { get; set; } = new List<CreateProductsItem>();
    }
}
