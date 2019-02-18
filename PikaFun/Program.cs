using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel.Models;

namespace PikaFun
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using (var db = new PikabuContext())
            {
                var top1000Commenters = await db.Comments
                    .Where(c => c.DateTimeUtc >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(c => c.UserName)
                    .OrderByDescending(grouping => grouping.Count())
                    .Take(1000)
                    .Select(comments => new { User = comments.Key, Comments = comments.Count() })
                    .ToArrayAsync();
            }
        }
    }
}
