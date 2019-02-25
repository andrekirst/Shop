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

namespace ProductSearchService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ILogger<ProductController> _logger;
        private readonly IProductsRepository _repository;

        public ProductController(
            IProductsRepository repository,
            ILogger<ProductController> logger)
        {
            _logger = logger;
            _repository = repository;
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

                return Ok(product);
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
