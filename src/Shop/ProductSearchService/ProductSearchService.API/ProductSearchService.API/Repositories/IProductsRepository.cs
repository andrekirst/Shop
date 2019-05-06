using ProductSearchService.API.Events;
using ProductSearchService.API.Model;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProductSearchService.API.Repositories
{
    public interface IProductsRepository
    {
        Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken);

        Task<List<Product>> Search(string filter, CancellationToken cancellationToken);

        Task<bool> CreateProduct(string productnumber, string name, string description);

        Task<bool> UpdateProductName(string productnumber, string name);

        Task<bool> CreateProducts(List<CreateProductsItem> products);
    }
}
