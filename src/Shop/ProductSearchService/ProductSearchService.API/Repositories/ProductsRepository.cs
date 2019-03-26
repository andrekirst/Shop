using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ProductSearchService.API.Model;
using static Elasticsearch.Net.PostData;

namespace ProductSearchService.API.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        public ProductsRepository(
            ILogger<ProductsRepository> logger,
            IElasticLowLevelClient client)
        {
            Logger = logger;
            ElasticClient = client;
        }

        private string Index => "productssearch";

        private string Type => "products";

        private ILogger<ProductsRepository> Logger { get; }
        
        private IElasticLowLevelClient ElasticClient { get; }

        public async Task<List<Product>> GetProductsByFilter(string filter, CancellationToken cancellationToken)
        {
            var response = await ElasticClient.SearchAsync<StringResponse>(
                index: Index,
                type: Type,
                body: Serializable(o:new
                    {
                        query = new
                        {
                            query_string = new
                            {
                                query = $"*{filter}*"
                            }
                        }
                    }),
                ctx: cancellationToken);

            return response.Success
                ? JObject.Parse(json: response.Body)[propertyName: "hits"][key: "hits"]
                    .Select(selector: s => s[key: "_source"].ToObject<Product>())
                    .ToList()
                : null;
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            var response = await ElasticClient.GetAsync<StringResponse>(
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
