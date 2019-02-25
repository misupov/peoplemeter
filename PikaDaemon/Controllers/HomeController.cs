using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PikaDaemon.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        // GET api/statistics
        [HttpGet()]
        public async Task<string> Get()
        {
            return "HI!";
        }
    }
}
