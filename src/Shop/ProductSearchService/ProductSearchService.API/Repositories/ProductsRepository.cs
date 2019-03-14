using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ProductSearchService.API.Model;

namespace ProductSearchService.API.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ILogger<ProductsRepository> _logger;
        private readonly IElasticLowLevelClient _client;

        public ProductsRepository(
            ILogger<ProductsRepository> logger,
            IElasticLowLevelClient client)
        {
            _logger = logger;
            _client = client;
        }

        private string Index => "productssearch";

        private string Type => "products";

        public async Task<List<Product>> GetProductsByFilter(string filter, CancellationToken cancellationToken)
        {
            var response = await _client.SearchAsync<StringResponse>(
                index: Index,
                type: Type,
                body: PostData.Serializable(
                    o: new
                    {
                        query = new
                        {
                            query_string = new
                            {
                                query = $"*{filter}*"
                            }
                        }
                    }), ctx: cancellationToken);
            
            if (response.Success)
            {
                JObject parsedBody = JObject.Parse(json: response.Body);
                return parsedBody[propertyName: "hits"][key: "hits"]
                    .Select(selector: s => s["_source"].ToObject<Product>())
                    .ToList();
            }

            return null;
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            var response = await _client.GetAsync<StringResponse>(
                index: Index,
                type: Type,
                id: productnumber,
                ctx: cancellationToken);

            if (response.Success)
            {
                JObject parsedBody = JObject.Parse(json: response.Body);

                bool productFound = parsedBody.Value<bool>(key: "found");

                if (productFound)
                {
                    return parsedBody
                        .SelectToken(path: "_source")
                        .ToObject<Product>();
                }
            }

            return null;
        }
    }
}
