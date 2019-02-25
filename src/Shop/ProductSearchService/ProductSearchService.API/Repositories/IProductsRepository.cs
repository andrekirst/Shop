using ProductSearchService.API.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductSearchService.API.Repositories
{
    public interface IProductsRepository
    {
        Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken);

        Task<List<Product>> GetProductsByFilter(string filter, CancellationToken cancellationToken);
    }
}
