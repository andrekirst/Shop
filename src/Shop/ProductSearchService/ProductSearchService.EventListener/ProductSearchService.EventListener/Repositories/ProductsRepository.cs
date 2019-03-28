using System.Threading.Tasks;
using Elasticsearch.Net;

namespace ProductSearchService.EventListener.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ElasticLowLevelClient _client;

        public ProductsRepository(ElasticLowLevelClient client)
        {
            _client = client;
        }

        public async Task<bool> CreateProduct(string productnumber, string name, string description)
        {
            StringResponse response = await _client.IndexAsync<StringResponse>(
                index: "productssearch",
                type: "products",
                id: productnumber,
                body: PostData.Serializable(o: new
                {
                    Productnumber = productnumber,
                    Name = name,
                    Description = description
                }));
            return response.Success;
        }
    }
}
