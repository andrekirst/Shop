using Microsoft.EntityFrameworkCore;
using ProductSearchService.EventListener.DataAccess;
using ProductSearchService.EventListener.Model;
using Serilog;
using System.Threading.Tasks;

namespace ProductSearchService.EventListener.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ProductSearchDbContext _dbContext;

        public ProductsRepository(ProductSearchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> CreateProduct(string productnumber, string name, string description)
        {
            try
            {
                bool existsProduct = await _dbContext.Products.AnyAsync(predicate: product => product.Productnumber == productnumber);

                if (existsProduct)
                {
                    return false;
                }

                await _dbContext.Products.AddAsync(entity: new Product
                {
                    Productnumber = productnumber,
                    Name = name,
                    Description = description
                });
                await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: true);
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error(exception: ex, messageTemplate: ex.Message);
                return false;
            }
        }
    }
}
