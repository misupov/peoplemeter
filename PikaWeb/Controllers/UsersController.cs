using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PikaModel;
using PikaWeb.Controllers.DataTransferObjects;

namespace PikaWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMemoryCache _cache;

        public UsersController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

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
            var key = $"{CacheKeys.TopUsers}/{users}/{days}";
            if (!_cache.TryGetValue(key, out var topUsers))
            {
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(60));

                var cacheEntry = await GetTopNonCached();
                _cache.Set(key, cacheEntry, cacheEntryOptions);
                return cacheEntry;
            }

            return (IEnumerable<TopDTO>) topUsers;

            async Task<TopDTO[]> GetTopNonCached()
            {
                using (var db = new PikabuContext())
                {
                    return await db.Comments
                        .Where(c => c.DateTimeUtc >= DateTime.UtcNow.AddDays(-days))
                        .Select(c => new {c.UserName, c.DateTimeUtc })
                        .GroupBy(c => c.UserName)
                        .OrderByDescending(grouping => grouping.Count())
                        .Take(users)
                        .Select(comments => new TopDTO {User = comments.Key, Comments = comments.Count()})
                        .ToArrayAsync();
                }
            }
        }
    }
}