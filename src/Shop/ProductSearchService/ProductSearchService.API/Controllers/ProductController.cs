using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Repositories;
using Infrastructure.Messaging;
using ProductSearchService.API.Events;
using AutoMapper;
using ProductSearchService.API.Commands;
using System;

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
                await PublishProductSelectedEvent(product);

                return Ok(product);
            }
            catch (TaskCanceledException exception)
            {
                _logger.LogError(exception, message: $"GetByProductnumber \"{productnumber}\" cancelled");
            }
            return NotFound();
        }

        private async Task PublishProductSelectedEvent(Product product)
        {
            ProductSelectedEvent productSelected = new ProductSelectedEvent(
                                messageId: Guid.NewGuid(),
                                productnumber: product.Productnumber,
                                name: product.Name,
                                description: product.Description);
            await _messagePublisher.PublishMessageAsync(
                messageType: productSelected.MessageType,
                message: productSelected,
                routingKey: "SearchLog");
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

                return Ok(products);
            }
            catch (TaskCanceledException exception)
            {
                _logger.LogError(exception, message: $"GetByFilter \"{filter}\" cancelled");
            }

            return NotFound();
        }
    }
}
