using System;
using System.Collections.Generic;
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
    public class UsersController : ControllerBase
    {
        // GET api/users/all
        [HttpGet("all")]
        public async Task<IEnumerable<string>> Get()
        {
            using (var db = new PikabuContext())
            {
                return await db.Users.Select(c => c.UserName).OrderBy(c => c).ToArrayAsync();
            }
        }

        // GET api/users/top/{users}/{days}
        [HttpGet("top/{users}/{days}")]
        public async Task<IEnumerable<TopDTO>> GetTop(int users, int days)
        {
            using (var db = new PikabuContext())
            {
                return await db.Comments
                    .Where(c => c.DateTimeUtc >= DateTime.UtcNow.AddDays(-days))
                    .GroupBy(c => c.User)
                    .OrderByDescending(grouping => grouping.Count())
                    .Take(users)
                    .Select(comments => new TopDTO { User = comments.Key.UserName, Comments = comments.Count()})
                    .ToArrayAsync();
            }
        }
    }
}