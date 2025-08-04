using System.Security.Claims;

namespace KwiatLuxeRESTAPI.Services.Data
{
    public static class UserInformation
    {
        public static int? GetCurrentUserId(ClaimsPrincipal nameIdentifier)
        {
            var identity = nameIdentifier.FindFirst(ClaimTypes.NameIdentifier);
            if (identity == null)
            {
                return null;
            }
            return int.Parse(identity.Value);
        }

        public static string? GetCurrentUsername(ClaimsPrincipal name)
        {
            return name.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string? GetCurrentMail(ClaimsPrincipal mail)
        {
            var identityMail = mail.FindFirst(ClaimTypes.NameIdentifier);
            return identityMail?.Value;
        }

        public static string? GetCurrentUserRole(ClaimsPrincipal role)
        {
            var identityRole = role.FindFirst(ClaimTypes.NameIdentifier);
            return identityRole?.Value;
        }

        public static bool IsAdmin(ClaimsPrincipal role)
        {
            var identityAdmin = role.FindFirst(ClaimTypes.NameIdentifier);
            return identityAdmin == null || identityAdmin.Value == "Admin";
        }
    }
}
