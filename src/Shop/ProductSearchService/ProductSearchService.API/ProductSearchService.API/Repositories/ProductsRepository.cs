using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            IElasticClientSettings elasticClientSettings)
        {
            Logger = logger;
            ElasticClientSettings = elasticClientSettings;
            InitializeElasticClient();
        }

        private void InitializeElasticClient()
        {
            var elasticNode = new Uri(uriString: ElasticClientSettings.Uri);
            var elasticConfiguration = new ConnectionConfiguration(uri: elasticNode)
                .RequestTimeout(timeout: ElasticClientSettings.RequestTimoutInMinutes.Minutes())
                .EnableHttpCompression(enabled: ElasticClientSettings.EnableHttpCompression)
                .EnableHttpPipelining(enabled: ElasticClientSettings.EnableHttpPipelining)
                .PrettyJson(b: ElasticClientSettings.PrettyJson);
            ElasticClient = new ElasticLowLevelClient(settings: elasticConfiguration);
        }

        private ILogger<ProductsRepository> Logger { get; }

        private IElasticClientSettings ElasticClientSettings { get; }

        private IElasticLowLevelClient ElasticClient { get; set; }

        public async Task<List<Product>> Search(string filter, CancellationToken cancellationToken)
        {
            Logger.LogInformation(message: $"ProductsRepository: Call SearchAsync for filter \"{filter}\"");

            var queryBody = Serializable(o: new
            {
                query = new
                {
                    multi_match = new
                    {
                        fields = new[] { $"{nameof(Product.Name)}^2", nameof(Product.Description) },
                        query = filter,
                        fuzziness = 10
                    }
                }
            });

            Logger.LogDebug(message: $"QueryBody: \"{JsonConvert.SerializeObject(queryBody)}\"");

            var response = await ElasticClient.SearchAsync<StringResponse>(
                    index: Index,
                    body: queryBody,
                    ctx: cancellationToken);

            if (!response.Success)
            {
                Logger.LogError(message: response.Body);
                Logger.LogDebug(message: response.DebugInformation);
            }

            // TODO Json interface

            var result = response.Success
                ? JObject.Parse(json: response.Body)[propertyName: "hits"][key: "hits"]
                    .Select(selector: s => s[key: "_source"].ToObject<Product>())
                    .ToList()
                : null;
            return result;
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            Logger.LogInformation(message: $"ProductsRepository: Call GetAsync to index \"{Index}\", type \"{Type}\" and id \"{productnumber}\"");
            var response = await ElasticClient.GetAsync<StringResponse>(
                    index: Index,
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

            Logger.LogError(message: response.Body);
            Logger.LogDebug(message: response.DebugInformation);

            return null;
        }

        public async Task<bool> CreateProduct(string productnumber, string name, string description)
        {
            StringResponse response = await ElasticClient.IndexAsync<StringResponse>(
                index: Index,
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
