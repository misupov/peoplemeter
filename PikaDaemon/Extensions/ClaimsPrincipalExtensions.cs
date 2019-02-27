using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PikaDaemon.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsAdministrator(this ClaimsPrincipal principal)
        {
            return principal.IsInRole("Administrators");
        }
    }
}
