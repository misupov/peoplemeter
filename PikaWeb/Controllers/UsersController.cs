using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PikaModel;

namespace PikaWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        // GET api/users
        [HttpGet]
        public async Task<IEnumerable<string>> Get()
        {
            using (var db = new PikabuContext())
            {
                return await db.Users.Select(c => c.UserName).OrderBy(c => c).ToArrayAsync();
            }
        }
    }
}