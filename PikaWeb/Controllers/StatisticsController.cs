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
    public class StatisticsController : ControllerBase
    {
        // GET api/statistics
        [HttpGet()]
        public async Task<StatisticsDTO> Get()
        {
            using (var db = new PikabuContext())
            {
                var commentsCount = await db.Comments.CountAsync();
                var usersCount = await db.Users.CountAsync();
                var storiesCount = await db.Stories.CountAsync();
                var fetcherStats = await db.FetcherStats.ToArrayAsync();
                return new StatisticsDTO()
                {
                    CommentsCount = commentsCount,
                    UsersCount = usersCount,
                    StoriesCount = storiesCount,
                    Fetchers = fetcherStats.Select(f => new StatisticsDTO.StatisticsFetchersDTO()
                    {
                        FetcherName = f.FetcherName,
                        StoriesPerSecondForLastHour = f.StoriesPerSecondForLastHour,
                        StoriesPerSecondForLastMinute = f.StoriesPerSecondForLastMinute
                    }).ToArray()
                };
            }
        }
    }
}
