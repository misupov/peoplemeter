using System;
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
        // GET api/comments/lam0x86
        [HttpGet("{userName}")]
        public async Task<CommentDTO[]> Get(string userName)
        {
            using (var db = new PikabuContext())
            {
                return await db.Comments
                    .Include(comment => comment.Story.StoryId)
                    .Where(c => c.User.UserName == userName)
                    .Select(c => new CommentDTO
                    {
                        StoryId = c.Story.StoryId,
                        CommentId = c.CommentId,
                        ParentId = c.ParentId,
                        DateTimeUtc = c.DateTimeUtc,
                        CommentBody = c.CommentBody
                    })
                    .ToArrayAsync();
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

    public class CommentDTO
    {
        public int StoryId { get; set; }
        public long CommentId { get; set; }
        public long ParentId { get; set; }
        public DateTime DateTimeUtc { get; set; }
        public string CommentBody { get; set; }
    }
}
