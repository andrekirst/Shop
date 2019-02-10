using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductSearchService.API.DataAccess;
using ProductSearchService.API.Model;
using System.Threading.Tasks;

namespace ProductSearchService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly ProductSearchDbContext _dbContext;

        public ProductController(ProductSearchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        [Route("{productnumber}", Name = "GetByProductnumber")]
        public async Task<ActionResult<Product>> GetByProductnumber(string productnumber)
        {
            var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Productnumber == productnumber);
            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }
    }
}
