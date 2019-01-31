using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

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

        // GET api/comments/5
        [HttpGet("{id}")]
        public ActionResult<string> Get(int id)
        {
            return "value";
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
