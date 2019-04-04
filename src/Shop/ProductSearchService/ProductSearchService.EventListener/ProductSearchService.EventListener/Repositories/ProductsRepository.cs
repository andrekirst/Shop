using System.Threading.Tasks;
using Elasticsearch.Net;

namespace ProductSearchService.EventListener.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private const string Index = "productssearch";
        private const string Type = "products";

        public ProductsRepository(ElasticLowLevelClient client)
        {
            Client = client;
        }

        private ElasticLowLevelClient Client { get; }

        public async Task<bool> CreateProduct(string productnumber, string name, string description)
        {
            StringResponse response = await Client.IndexAsync<StringResponse>(
                index: Index,
                type: Type,
                id: productnumber,
                body: PostData.Serializable(o: new
                {
                    Productnumber = productnumber,
                    Name = name,
                    Description = description
                }));
            return response.Success;
        }

        public async Task<bool> UpdateProductName(string productnumber, string name)
        {
            var response = await Client.UpdateAsync<StringResponse>(
                index: Index,
                type: Type,
                id: productnumber,
                body: PostData.Serializable(o: new
                {
                    doc = new
                    {
                        Name = name
                    }
                }));
            return response.Success;
        }
    }
}
