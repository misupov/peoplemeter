using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PikaModel.Models;

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
