using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using PikaModel;

namespace PikaWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly PikabuContext _db;

        public ChartModel ChartModel { get; private set; }

        public IndexModel(PikabuContext db)
        {
            _db = db;
        }

        public async Task OnGetAsync()
        {
            var days = 7;
            var users = 10;

            var tops = await _db.Comments
                .Where(c => c.DateTimeUtc >= DateTime.UtcNow.AddDays(-days))
                .GroupBy(c => c.User)
                .OrderByDescending(grouping => grouping.Count())
                .Take(users)
                .Select(comments => new { User = comments.Key.UserName, Comments = comments.Count() })
                .ToArrayAsync();

            ChartModel = new ChartModel()
            {
                labels = tops.Select(top => top.User).ToArray(),
                datasets = new[] {new ChartDataSet() {data = tops.Select(top => top.Comments).ToArray(), label = "Комментариев за " + days + " дней"}}
            };

        }
    }

    public class ChartModel
    {
        public string[] labels { get; set; }
        public ChartDataSet[] datasets { get; set; }
    }

    public class ChartDataSet
    {
        public string label { get; set; }

        public int[] data { get; set; }
    }
}
