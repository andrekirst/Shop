using FluentTimeSpan;
using Microsoft.AspNetCore.Mvc;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ProductSearchService.API.Infrastructure;
using ProductSearchService.API.Logging;

namespace ProductSearchService.API.Controllers
{
    [Route(template: "api/v{version:apiVersion}")]
    [ApiController]
    [ApiVersion(version: "1.0")]
    [ApiExplorerSettings(GroupName = "Products")]
    public class ProductController : ControllerBase
    {
        public ProductController(
            IProductsRepository repository,
            IMessagePublisher messagePublisher,
            ICache cache,
            IShopApiLogging logging,
            ICorrelationIdFactory correlationIdGenerator)
        {
            Repository = repository;
            MessagePublisher = messagePublisher;
            Cache = cache;
            Logging = logging;
            CorrelationIdFactory = correlationIdGenerator;
        }

        private IProductsRepository Repository { get; }

        private IMessagePublisher MessagePublisher { get; }

        private ICache Cache { get; }

        private IShopApiLogging Logging { get; }
        
        private ICorrelationIdFactory CorrelationIdFactory { get; }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route(template: "product/{productnumber}", Name = nameof(GetByProductnumber))]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            string correlationId = CorrelationIdFactory.Build();
            string cacheKey = $"ProductSearchService.Product[Productnumber=\"{productnumber}\"]";
            try
            {
                Product product = await Logging.LogStartAndEnd(
                    func: () => Cache.Get<Product>(key: cacheKey),
                    logState: LogState.Info,
                    messageStart: "Get Value from Cache",
                    messageEnd: "Get Value from Cache",
                    controllerName: nameof(ProductController),
                    actionName: nameof(GetByProductnumber),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(productnumber), productnumber }
                    });
                bool isProductCached = product != null;

                if (isProductCached)
                {
                    return Ok(value: product);
                }

                product = await Logging.LogStartAndEnd(
                    func: async () => await Repository.GetProductByProductnumber(
                        productnumber: productnumber,
                        cancellationToken: cancellationToken),
                    logState: LogState.Info,
                    messageStart: "Get Value from Repository",
                    messageEnd: "Get Value from Repository",
                    controllerName: nameof(ProductController),
                    actionName: nameof(GetByProductnumber),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(productnumber), productnumber }
                    });

                if (product == null)
                {
                    return NotFound();
                }

                await QueueProductSelectedEvent(product: product);

                Cache.Set(
                    key: cacheKey,
                    value: product,
                    duration: 24.Hours());

                return Ok(value: product);

            }
            catch (TaskCanceledException exception)
            {
                _ = Logging.LogError(
                    exception: exception,
                    message: $"{nameof(GetByProductnumber)} \"{productnumber}\" cancelled",
                    controllerName: nameof(ProductController),
                    actionName: nameof(GetByProductnumber),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(productnumber), productnumber }
                    });
            }
            return NotFound();
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(List<Product>))]
        [Route(template: "products/{filter}", Name = nameof(Search))]
        public async Task<ActionResult<List<Product>>> Search(string filter, CancellationToken cancellationToken)
        {
            string correlationId = CorrelationIdFactory.Build();
            string cacheKey = $"ProductSearchService.Products[filter=\"{filter}\"]";
            try
            {
                List<Product> products = await Logging.LogStartAndEnd(
                    func: () => Cache.Get<List<Product>>(key: cacheKey),
                    logState: LogState.Info,
                    messageStart: "Get Value from Cache",
                    messageEnd: "Get Value from Cache",
                    controllerName: nameof(ProductController),
                    actionName: nameof(Search),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(filter), filter }
                    });
                bool areProductsCached = products != null;

                if (areProductsCached)
                {
                    return Ok(value: products);
                }

                products = await Logging.LogStartAndEnd(
                    func: async () => await Repository.Search(
                        filter: filter,
                        cancellationToken: cancellationToken),
                    logState: LogState.Info,
                    messageStart: "Get Value from Repository",
                    messageEnd: "Get Value from Repository",
                    controllerName: nameof(ProductController),
                    actionName: nameof(Search),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(filter), filter }
                    });

                await QueueProductsSearchedEvent(products: products, filter: filter);

                if (products == null || !products.Any())
                {
                    return NotFound(value: null);
                }

                Cache.Set(
                    key: cacheKey,
                    value: products,
                    duration: 1.Minutes());

                return Ok(value: products);

            }
            catch (TaskCanceledException exception)
            {
                _ = Logging.LogError(
                    exception: exception,
                    message: $"{nameof(Search)} \"{filter}\" cancelled",
                    controllerName: nameof(ProductController),
                    actionName: nameof(Search),
                    httpVerb: "GET",
                    apiVersion: "1.0",
                    correlationId: correlationId,
                    controller: this,
                    parameters: new Dictionary<string, object>
                    {
                        { nameof(filter), filter }
                    });
            }
            return NotFound();
        }

        private Task QueueProductsSearchedEvent(List<Product> products, string filter) =>
            MessagePublisher.SendEventAsync(
                @event: new ProductsSearchedEvent(
                    filter: filter,
                    productsFound: products != null && products.Any(),
                    numberOfProductsFound: products?.Count ?? 0),
                messageType: "Event:ProductsSearchedEvent",
                exchange: "SearchLog");

        private Task QueueProductSelectedEvent(Product product) =>
            MessagePublisher.SendEventAsync(
                @event: new ProductSelectedEvent(
                    productnumber: product.Productnumber,
                    name: product.Name,
                    description: product.Description),
                messageType: "Event:ProductSelectedEvent",
                exchange: "SearchLog");
    }
}
