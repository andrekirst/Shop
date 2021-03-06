﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ProductSearchService.API.Events;
using ProductSearchService.API.Model;
using Shop.Infrastructure.Infrastructure.Json;
using static Elasticsearch.Net.PostData;
using IDateTimeProvider = Shop.Infrastructure.Infrastructure.IDateTimeProvider;

namespace ProductSearchService.API.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private const string Index = "productsearch";

        public ProductsRepository(
            ILogger<ProductsRepository> logger,
            IElasticClientSettings elasticClientSettings,
            IJsonSerializer jsonSerializer,
            IDateTimeProvider dateTimeProvider)
        {
            Logger = logger;
            ElasticClientSettings = elasticClientSettings;
            JsonSerializer = jsonSerializer;
            DateTimeProvider = dateTimeProvider;
            InitializeElasticClient();
            CreateIndexIfNotExists();
        }

        private void CreateIndexIfNotExists()
        {
            StringResponse response = ElasticClient.IndicesExists<StringResponse>(index: Index);
            if (response.HttpStatusCode == 404)
            {
                StringResponse createIndexResponse = ElasticClient.IndicesCreate<StringResponse>(
                    index: Index,
                    body: Serializable(o: new
                    {
                        settings = new
                        {
                            index = new
                            {
                                number_of_shards = ElasticClientSettings.NumberOfShards,
                                number_of_replicas = ElasticClientSettings.NumberOfReplicas
                            }
                        }
                    }));

                if (createIndexResponse.Success &&
                    createIndexResponse.HttpStatusCode == 200)
                {
                    ElasticClient.IndicesPutMapping<StringResponse>(
                        index: Index,
                        body: Serializable(o: new
                        {
                            properties = new
                            {
                                Productnumber = new
                                {
                                    type = "text"
                                },
                                Name = new
                                {
                                    type = "text"
                                },
                                Description = new
                                {
                                    type = "text"
                                },
                                ToIndexAddedAt = new
                                {
                                    type = "date"
                                }
                            }
                        }));
                }
            }

        }

        private void InitializeElasticClient()
        {
            Uri elasticNode = new Uri(uriString: ElasticClientSettings.Uri);
            ConnectionConfiguration elasticConfiguration = new ConnectionConfiguration(uri: elasticNode)
                .RequestTimeout(timeout: ElasticClientSettings.RequestTimoutInMinutes.Minutes())
                .EnableHttpCompression(enabled: ElasticClientSettings.EnableHttpCompression)
                .EnableHttpPipelining(enabled: ElasticClientSettings.EnableHttpPipelining)
                .PrettyJson(b: ElasticClientSettings.PrettyJson);
            ElasticClient = new ElasticLowLevelClient(settings: elasticConfiguration);
        }

        private ILogger<ProductsRepository> Logger { get; }

        private IElasticClientSettings ElasticClientSettings { get; }

        private IJsonSerializer JsonSerializer { get; }

        private IDateTimeProvider DateTimeProvider { get; }

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
                        fields = new[]
                        {
                            $"{nameof(Product.Productnumber)}^3",
                            $"{nameof(Product.Name)}^2",
                            nameof(Product.Description)
                        },
                        query = filter,
                        fuzziness = 10
                    }
                },
                sort = new
                {
                    _score = new { order = "desc" }
                }
            });

            Logger.LogDebug(message: $"QueryBody: \"{JsonSerializer.Serialize(value: queryBody)}\"");

            StringResponse response = await ElasticClient.SearchAsync<StringResponse>(
                    index: Index,
                    body: queryBody,
                    ctx: cancellationToken);

            if (!response.Success)
            {
                Logger.LogError(message: response.Body);
                Logger.LogDebug(message: response.DebugInformation);
            }

            // TODO Json interface
            List<Product> result = response.Success
                ? JObject.Parse(json: response.Body)[propertyName: "hits"][key: "hits"]
                    .Select(selector: s => s[key: "_source"].ToObject<Product>())
                    .ToList()
                : null;
            return result;
        }

        public async Task<Product> GetProductByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            Logger.LogInformation(message: $"ProductsRepository: Call GetAsync to index \"{Index}\" and id \"{productnumber}\"");
            StringResponse response = await ElasticClient.GetAsync<StringResponse>(
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
                body: Serializable(o: new
                {
                    Productnumber = productnumber,
                    Name = name,
                    Description = description,
                    ToIndexAddedAt = DateTimeProvider.Now
                }));
            return
                response.Success &&
                response.HttpStatusCode == 201;
        }

        public async Task<bool> UpdateProductName(string productnumber, string name)
        {
            StringResponse response = await ElasticClient.UpdateAsync<StringResponse>(
                index: Index,
                id: productnumber,
                body: Serializable(o: new
                {
                    doc = new
                    {
                        Name = name
                    }
                }));
            return response.Success;
        }

        public async Task<bool> CreateProducts(List<CreateProductsItem> products)
        {
            int numberOfProducts = products.Count;

            object[] items = new object[numberOfProducts * 2];
            for (int i = 0; i < numberOfProducts; i++)
            {
                items[i * 2] = new { index = new { _index = Index, _id = products[index: i].Productnumber } };
                items[(i * 2) + 1] = new
                {
                    products[index: i].Productnumber,
                    products[index: i].Name,
                    products[index: i].Description,
                    ToIndexAddedAt = DateTimeProvider.Now
                };
            }
            StringResponse response = await ElasticClient.BulkAsync<StringResponse>(body: MultiJson(listOfObjects: items));
            return response.Success;
        }
    }
}
