using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    [Route(template: "api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        public ProductController(
            IProductsRepository repository,
            ILogger<ProductController> logger,
            IMessagePublisher messagePublisher)
        {
            Logger = logger;
            Repository = repository;
            MessagePublisher = messagePublisher;
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route(template: "{productnumber}", Name = "GetByProductnumber")]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            try
            {
                var product = await Repository.GetProductByProductnumber(
                    productnumber: productnumber,
                    cancellationToken: cancellationToken);

                if (product != null)
                {
                    await PublishProductSelectedEvent(product: product);
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
        [Route(template: "byfilter/{filter}", Name = "GetByFilter")]
        public async Task<ActionResult<List<Product>>> GetByFilter(string filter, CancellationToken cancellationToken)
        {
            try
            {
                var products = await Repository.GetProductsByFilter(
                    filter: filter,
                    cancellationToken: cancellationToken);

                await PublishProductsSearchedEvent(products: products, filter: filter);

                if (products != null && products.Any())
                {
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


        private ILogger<ProductController> Logger { get; }

        private IProductsRepository Repository { get; }

        private IMessagePublisher MessagePublisher { get; }
    }
}
