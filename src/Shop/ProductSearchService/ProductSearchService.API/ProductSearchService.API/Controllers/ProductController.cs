using FluentTimeSpan;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductSearchService.API.Controllers
{
    [Route(template: "api")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        public ProductController(
            IProductsRepository repository,
            ILogger<ProductController> logger,
            IMessagePublisher messagePublisher,
            ICache cache)
        {
            Logger = logger;
            Repository = repository;
            MessagePublisher = messagePublisher;
            Cache = cache;
        }

        private ILogger<ProductController> Logger { get; }

        private IProductsRepository Repository { get; }

        private IMessagePublisher MessagePublisher { get; }
        
        private ICache Cache { get; }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route(template: "product/{productnumber}", Name = nameof(GetByProductnumber))]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            string cacheKey = $"ProductSearchService.Product[Productnumber=\"{productnumber}\"]";
            try
            {
                var product = Cache.Get<Product>(key: cacheKey);
                bool isCached = product != null;
                
                if (!isCached)
                {
                    product = await Repository.GetProductByProductnumber(
                        productnumber: productnumber,
                        cancellationToken: cancellationToken);
                }

                if (product != null)
                {
                    _ = Task.Factory.StartNew(() => QueueProductSelectedEvent(product: product));
                    if (!isCached)
                    {
                        _ = Task.Factory.StartNew(() => Cache.Set(
                            key: cacheKey,
                            value: product,
                            duration: 24.Hours())); 
                    }

                    return Ok(value: product);
                }

                return NotFound();
            }
            catch (TaskCanceledException exception)
            {
                Logger.LogError(exception: exception, message: $"GetByProductnumber \"{productnumber}\" cancelled");
            }
            return NotFound();
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(List<Product>))]
        [Route(template: "products/{filter}", Name = nameof(Search))]
        public async Task<ActionResult<List<Product>>> Search(string filter, CancellationToken cancellationToken)
        {
            string cacheKey = $"ProductSearchService.Products[filter=\"{filter}\"]";
            try
            {
                var products = Cache.Get<List<Product>>(key: cacheKey);
                bool isCached = products != null;
                if (!isCached)
                {
                    products = await Repository.Search(
                        filter: filter,
                        cancellationToken: cancellationToken);
                }

                _ = Task.Factory.StartNew(() => QueueProductsSearchedEvent(products: products, filter: filter));

                if (products != null && products.Any())
                {
                    if (!isCached)
                    {
                        Cache.Set(
                           key: cacheKey,
                           value: products,
                           duration: 1.Minutes());
                    }
                    
                    return Ok(value: products);
                }

                return NotFound(null);
            }
            catch (TaskCanceledException exception)
            {
                Logger.LogError(exception: exception, message: $"{nameof(Search)} \"{filter}\" cancelled");
            }

            Logger.LogInformation(message: $"No data found for filter {filter}.");
            return NotFound();
        }

        private Task QueueProductsSearchedEvent(List<Product> products, string filter) =>
            MessagePublisher.SendEventAsync(
                @event: new ProductsSearchedEvent(
                filter: filter,
                productsFound: products != null && products.Any(),
                numberOfProductsFound: products?.Count ?? 0),
                messageType: "ProductsSearchedEvent");

        private Task QueueProductSelectedEvent(Product product) =>
            MessagePublisher.SendEventAsync(
                @event: new ProductSelectedEvent(
                    productnumber: product.Productnumber,
                    name: product.Name,
                    description: product.Description),
                messageType: "ProductSelectedEvent");
    }
}
