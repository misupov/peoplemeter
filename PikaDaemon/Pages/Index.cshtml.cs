using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaDaemon.Data;

namespace PikaDaemon.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<PikabuUser> _userManager;

        public IndexModel(UserManager<PikabuUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnGet()
        {
            var pikabuUser = await _userManager.GetUserAsync(User);
            if (pikabuUser != null)
            {
                await _userManager.AddToRoleAsync(pikabuUser, "Administrators");
            }
        }
    }
}
