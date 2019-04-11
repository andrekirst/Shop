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
            ICache<Product> productCache,
            ICache<List<Product>> productsCache)
        {
            Logger = logger;
            Repository = repository;
            MessagePublisher = messagePublisher;
            ProductCache = productCache;
            ProductsCache = productsCache;
        }

        private ILogger<ProductController> Logger { get; }

        private IProductsRepository Repository { get; }

        private IMessagePublisher MessagePublisher { get; }
        
        public ICache<Product> ProductCache { get; }
        
        public ICache<List<Product>> ProductsCache { get; }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route(template: "product/{productnumber}", Name = nameof(GetByProductnumber))]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            string cacheKey = $"ProductSearchService.Product[Productnumber=\"{productnumber}\"]";
            try
            {
                var product = ProductCache.Get(key: cacheKey)
                    ?? await Repository.GetProductByProductnumber(
                        productnumber: productnumber,
                        cancellationToken: cancellationToken);

                if (product != null)
                {
                    _ = Task.Factory.StartNew(() => PublishProductSelectedEvent(product: product));
                    _ = Task.Factory.StartNew(() => ProductCache.Set(key: cacheKey, value: product, duration: 1.Minutes()));

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
                var products = ProductsCache.Get(key: cacheKey)
                    ?? await Repository.Search(
                        filter: filter,
                        cancellationToken: cancellationToken);

                _ = Task.Factory.StartNew(() => PublishProductsSearchedEvent(products: products, filter: filter));

                if (products != null && products.Any())
                {
                    ProductsCache.Set(
                        key: cacheKey,
                        value: products,
                        duration: 1.Minutes());
                    return Ok(value: products);
                }

                return NotFound();
            }
            catch (TaskCanceledException exception)
            {
                Logger.LogError(exception: exception, message: $"GetByFilter \"{filter}\" cancelled");
            }

            Logger.LogInformation(message: $"No data found for filter {filter}.");
            return NotFound();
        }

        private Task PublishProductsSearchedEvent(List<Product> products, string filter)
        {
            ProductsSearchedEvent @event = new ProductsSearchedEvent(
                filter: filter,
                productsFound: products != null && products.Any(),
                numberOfProductsFound: products?.Count ?? 0);
            return MessagePublisher.SendMessageAsync(
                message: @event,
                messageType: "ProductsSearchedEvent");
        }

        private Task PublishProductSelectedEvent(Product product)
        {
            ProductSelectedEvent @event = new ProductSelectedEvent(
                    productnumber: product.Productnumber,
                    name: product.Name,
                    description: product.Description);
            return MessagePublisher.SendMessageAsync(
                message: @event,
                messageType: "ProductSelectedEvent");
        }
    }
}
