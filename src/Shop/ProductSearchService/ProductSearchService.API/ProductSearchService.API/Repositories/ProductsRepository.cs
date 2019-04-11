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
        private const string Index = "productssearch";
        private const string Type = "products";

        public ProductsRepository(
            ILogger<ProductsRepository> logger,
            IElasticLowLevelClient client)
        {
            Logger = logger;
            ElasticClient = client;
        }

        private ILogger<ProductsRepository> Logger { get; }

        private IElasticLowLevelClient ElasticClient { get; }

        public async Task<List<Product>> Search(string filter, CancellationToken cancellationToken)
        {
            var response = await ElasticClient.SearchAsync<StringResponse>(
                    index: Index,
                    type: Type,
                    body: Serializable(o: new
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

            // TODO Json interface
            return response.Success
                ? JObject.Parse(json: response.Body)[propertyName: "hits"][key: "hits"]
                    .Select(selector: s => s[key: "_source"].ToObject<Product>())
                    .ToList()
                : null;
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            Logger.LogInformation(message: $"Call GetAsync to index \"{Index}\", type \"{Type}\" and id \"{productnumber}\"");
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

        public async Task<bool> CreateProduct(string productnumber, string name, string description)
        {
            StringResponse response = await ElasticClient.IndexAsync<StringResponse>(
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
            var response = await ElasticClient.UpdateAsync<StringResponse>(
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
