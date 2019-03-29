using System.Collections.Generic;
using ProductSearchService.API.Controllers;
using ProductSearchService.API.Model;
using Xbehave;

namespace ProductSearchService.API.Tests
{
    public class ProductControllerSearchTests
    {
        [Scenario(DisplayName = @"Search for ""queen"". It exists 2 Products with ""queen"". Expect 2 Products in list.")]
        public void SearchQueenProducts(
            string filter,
            ProductController productController,
            List<Product> expectedProducts)
        {

        }
    }
}