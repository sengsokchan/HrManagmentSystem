using System.Text.RegularExpressions;

namespace HrManagementSystem.Application;

public static partial class PasswordPolicy
{
    public const int MinLength = 14;
    public const int MaxLength = 128;

    public static bool TryValidate(string? password, out string message)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            message = "Password is required.";
            return false;
        }

        var value = password.Trim();
        if (value.Length < MinLength)
        {
            message = $"Use a passphrase of at least {MinLength} characters (for example River-Coffee-Moon-Train-84).";
            return false;
        }

        if (value.Length > MaxLength)
        {
            message = $"Password must be at most {MaxLength} characters.";
            return false;
        }

        if (!value.Any(char.IsLetter))
        {
            message = "Password must include letters. Prefer a memorable passphrase.";
            return false;
        }

        if (!AllowedPattern().IsMatch(value))
        {
            message = "Password may use letters, numbers, spaces, and - _ ! . only.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    [GeneratedRegex(@"^[A-Za-z0-9 \-_!.]+$")]
    private static partial Regex AllowedPattern();
}
