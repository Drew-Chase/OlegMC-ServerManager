using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OlegMC.REST_API.View
{
    public class LoginModel : PageModel
    {
        private static readonly ChaseLabs.CLLogger.Interfaces.ILog log = Data.Global.Logger;
        public void OnGet()
        {
        }
    }
}
