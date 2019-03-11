using System.Threading.Tasks;

namespace ProductSearchService.EventListener.Repositories
{
    public interface IProductsRepository
    {
        Task<bool> CreateProduct(string productnumber, string name, string description);
    }
}
