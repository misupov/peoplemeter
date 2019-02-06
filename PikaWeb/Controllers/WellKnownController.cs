using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PikaWeb.Controllers
{
    [Route(".well-known/acme-challenge/N1RLdAv9CotNvJNrV1isQLJfXVx8bYcNnmpxUTvYLQ8")]
    [ApiController]
    public class WellKnownController : ControllerBase
    {
        // GET api/users
        [HttpGet]
        public async Task<string> Get()
        {
            return "N1RLdAv9CotNvJNrV1isQLJfXVx8bYcNnmpxUTvYLQ8.CtZTdwAmWHy-cn66DdUSzEFmfwAz6HLg9jNKkjzRoBU";
        }
    }
}