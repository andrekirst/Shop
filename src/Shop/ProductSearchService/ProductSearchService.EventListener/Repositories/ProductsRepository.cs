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
            var x = _dbContext.ChangeTracker;
            try
            {
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
                Log.Error(ex, ex.Message);
                return false;
            }
        }
    }
}
