using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Authdoc.Validators;

public static class EmailValidator
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] AllowedDomains = ["gmail.com", "outlook.com", "hotmail.com", "live.com"];

    public static string Normalize(string? email)
    {
        return email?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    public static bool IsValid(string email)
    {
        if (!HasValidFormat(email))
            return false;
        return IsDomainAllowed(email);
    }

    private static bool HasValidFormat(string email)
    {
        try
        {
            var address = new MailAddress(email);
            return address.Address == email && EmailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsDomainAllowed(string email)
    {
        string domain = email.Split('@')[1].ToLower();
        return AllowedDomains.Contains(domain);
    }
}
