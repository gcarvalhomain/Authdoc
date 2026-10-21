using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Authdoc.Validators;

public static class EmailValidator
{
    
    private const int MaxLength = 254;

    
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        email = email.Trim();

        if (email.Length > MaxLength)
        {
            return false;
        }
        
        if (email.Contains(".."))
        {
            return false;
        }
        
        // Layer 1: quick format filter
        if (!EmailRegex.IsMatch(email))
        {
            return false;
        }
        
        // Layer 2: the .NET analyzer checks the truth rules
        if (!MailAddress.TryCreate(email, out var address))
        {
            return false;
        }
        
        // Layer 3: what he understood must be the same as what was typed
        return address.Address == email;
    }

}
