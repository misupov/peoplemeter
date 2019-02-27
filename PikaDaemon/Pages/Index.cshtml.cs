using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PikaDaemon.Data;

namespace PikaDaemon.Pages
{
    public class IndexModel : PageModel
    {
        private readonly RoleManager<PikabuRole> _roleManager;
        private readonly UserManager<PikabuUser> _userManager;

        public IndexModel(RoleManager<PikabuRole> roleManager, UserManager<PikabuUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task OnGet()
        {
//            if (!_roleManager.Roles.Any(role => role.NormalizedName == "ADMINISTRATORS"))
//            {
//                await _roleManager.CreateAsync(new PikabuRole() {Name = "Administrators"});
//            }
//
//            var pikabuUser = await _userManager.GetUserAsync(User);
//            if (pikabuUser != null)
//            {
//                await _userManager.AddToRoleAsync(pikabuUser, "Administrators");
//                var claim = new Claim(ClaimTypes.Role, "Administrators");
//                await _userManager.AddClaimAsync(pikabuUser, claim);
//            }
        }
    }
}
