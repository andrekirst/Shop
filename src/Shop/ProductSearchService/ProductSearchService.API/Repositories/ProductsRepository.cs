using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ILogger<ProductsRepository> _logger;
        private readonly ProductSearchDbContext _dbContext;

        public ProductsRepository(
            ProductSearchDbContext dbContext,
            ILogger<ProductsRepository> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<List<Product>> GetProductsByFilter(string filter, CancellationToken cancellationToken)
        {
            var filterParameter = new SqlParameter("Filter", $"%{filter.Trim()}%");
            return await _dbContext.Products.FromSql(sql:
                    "SELECT [ProductId], [Productnumber], [Name], [Description] FROM [dbo].[Products]" +
                    "WHERE [Productnumber] LIKE @Filter " +
                    "OR [Name] LIKE @Filter " +
                    "OR [Description] LIKE @Filter",
                    filterParameter)
                    .ToListAsync(cancellationToken: cancellationToken);
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            return await _dbContext.Products.FirstOrDefaultAsync(
                predicate: p => p.Productnumber == productnumber,
                cancellationToken: cancellationToken);
        }
    }
}
