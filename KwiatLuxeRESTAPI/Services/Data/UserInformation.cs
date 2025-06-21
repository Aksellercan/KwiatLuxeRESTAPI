namespace KwiatLuxeRESTAPI.Services.Data
{
    public class UserInformation
    {
        public int GetCurrentUserId(string? nameIdentifier) 
        {
            if (nameIdentifier == null) 
            {
                return -1;
            }
            return int.Parse(nameIdentifier);
        }

        public string? GetCurrentUsername(string? name)
        {
            if (name == null)
            {
                return null;
            }
            return name;
        }

        public string? GetCurrentMail(string? mail)
        {
            if (mail == null)
            {
                return null;
            }
            return mail;
        }

        public string? getCurrentUserRole(string? role) 
        {
            if (role == null) 
            {
                return null; 
            }

            return role;
        }

        public bool IsAdmin(string? role) 
        {
            if (role != null && role != "Admin") 
            {
                return false;
            }
            return true;
        }
    }
}
