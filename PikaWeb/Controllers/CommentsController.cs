using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PikaModel.Models;
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
                    .Where(c => c.UserName == userName)
                    .Where(c => c.CommentId < skipTill)
                    .OrderByDescending(c => c.DateTimeUtc)
                    .Take(50)
                    .Select(c => new CommentDTO
                    {
                        StoryId = c.StoryId,
                        StoryTitle = c.Story.Title,
                        AvatarUrl = db.Users.Single(u => u.UserName == c.UserName).AvatarUrl,
                        CommentId = c.CommentId,
                        ParentId = c.ParentId,
                        DateTimeUtc = c.DateTimeUtc,
                        CommentBody = c.CommentContent.BodyHtml,
                        IsAuthor = c.UserName == c.Story.Author,
                        Rating = c.Rating
                    })
                    .ToArrayAsync();
            }
        }
    }
}
