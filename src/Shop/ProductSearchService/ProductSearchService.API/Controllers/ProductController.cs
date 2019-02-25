using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ProductSearchService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductSearchDbContext _dbContext;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            ProductSearchDbContext dbContext,
            ILogger<ProductController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(statusCode: 404)]
        [ProducesResponseType(statusCode: 200, Type = typeof(Product))]
        [Route("{productnumber}", Name = "GetByProductnumber")]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber, CancellationToken cancellationToken)
        {
            try
            {
                var product = await _dbContext.Products.FirstOrDefaultAsync(
                    predicate: p => p.Productnumber == productnumber,
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
                var filterParameter = new SqlParameter("Filter", $"%{filter.Trim()}%");
                var products = await _dbContext.Products.FromSql(sql:
                        "SELECT [ProductId], [Productnumber], [Name], [Description] FROM [dbo].[Products]" +
                        "WHERE [Productnumber] LIKE @Filter " +
                        "OR [Name] LIKE @Filter " +
                        "OR [Description] LIKE @Filter",
                        filterParameter)
                        .ToListAsync(cancellationToken: cancellationToken);

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
