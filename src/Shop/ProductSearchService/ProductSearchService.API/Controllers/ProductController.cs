﻿using Microsoft.AspNetCore.Mvc;
using ProductSearchService.API.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Repositories;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Events;
using System.Linq;

namespace ProductSearchService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly IProductsRepository _repository;
        private readonly IMessagePublisher _messagePublisher;

        public ProductController(
            IProductsRepository repository,
            ILogger<ProductController> logger,
            IMessagePublisher messagePublisher)
        {
            _logger = logger;
            _repository = repository;
            _messagePublisher = messagePublisher;
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route("{productnumber}", Name = "GetByProductnumber")]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _repository.GetProductByProductnumber(
                    productnumber: productnumber,
                    cancellationToken: cancellationToken);

                if (product != null)
                {
                    await PublishProductSelectedEvent(product: product);
                    return Ok(product);
                }

                return NotFound();
            }
            catch (TaskCanceledException exception)
            {
                _logger.LogError(exception, message: $"GetByProductnumber \"{productnumber}\" cancelled");
            }
            return NotFound();
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(List<Product>))]
        [Route("byfilter/{filter}", Name = "GetByFilter")]
        public async Task<ActionResult<List<Product>>> GetByFilter(string filter, CancellationToken cancellationToken)
        {
            try
            {
                var products = await _repository.GetProductsByFilter(
                    filter: filter,
                    cancellationToken: cancellationToken);

                await PublishProductsSearchedEvent(products, filter);
                return Ok(products);
            }
            catch (TaskCanceledException exception)
            {
                _logger.LogError(exception, message: $"GetByFilter \"{filter}\" cancelled");
            }

            _logger.LogInformation($"No data found for filter {filter}.");
            return NotFound();
        }

        private async Task PublishProductsSearchedEvent(List<Product> products, string filter)
        {
            ProductsSearchedEvent @event = new ProductsSearchedEvent(
                filter: filter,
                productsFound: products != null && products.Any(),
                numberOfProductsFound: products.Count);
            await _messagePublisher.SendMessageAsync(@event, "ProductsSearchedEvent");
        }

        private async Task PublishProductSelectedEvent(Product product)
        {
            ProductSelectedEvent @event = new ProductSelectedEvent(
                    productnumber: product.Productnumber,
                    name: product.Name,
                    description: product.Description);
            await _messagePublisher.SendMessageAsync(@event, "ProductSelectedEvent");
        }
    }
}
