using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PikaModel;
using PikaWeb.Controllers.DataTransferObjects;

namespace PikaWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentsController : ControllerBase
    {
        // GET api/comments/lam0x86?skipTill=545674
        [HttpGet("{userName}")]
        public async Task<CommentDTO[]> Get(string userName, long skipTill = long.MaxValue)
        {
            using (var db = new PikabuContext())
            {
                return await db.Comments
                    .Include(u => u.Story.Author)
                    .Where(c => c.User.UserName == userName)
                    .Where(c => c.CommentId < skipTill)
                    .OrderByDescending(c => c.DateTimeUtc)
                    .Take(50)
                    .Select(c => new CommentDTO
                    {
                        StoryId = c.StoryId,
                        StoryTitle = c.Story.Title,
                        CommentId = c.CommentId,
                        ParentId = c.ParentId,
                        DateTimeUtc = c.DateTimeUtc,
                        CommentBody = c.CommentBody,
                        IsAuthor = c.User.UserName == c.Story.Author
                    })
                    .ToArrayAsync();
            }
        }
    }
}
