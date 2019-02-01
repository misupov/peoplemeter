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
    public class CommentsController : ControllerBase
    {
        // GET api/comments
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/comments/lam0x86
        [HttpGet("{userName}")]
        public async Task<Comment[]> Get(string userName)
        {
            using (var db = new PikabuContext())
            {
                return await db.Comments.Where(c => c.User.UserName == userName).ToArrayAsync();
            }
        }

        // POST api/comments
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/comments/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/comments/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
