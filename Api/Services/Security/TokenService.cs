using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HrManagementSystem.Application;
using HrManagementSystem.Domain;

namespace HrManagementSystem.Infrastructure.Security;

public sealed class TokenService(IConfiguration configuration) : ITokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        configuration["Auth:SigningKey"] ?? "HrManagementSystem-Dev-Signing-Key-ChangeMe!");
    private readonly string _issuer = configuration["Auth:Issuer"] ?? "HrManagementSystem";
    private readonly string _audience = configuration["Auth:Audience"] ?? "HrManagementSystem.Clients";
    private readonly TimeSpan _lifetime = TimeSpan.FromHours(
        double.TryParse(configuration["Auth:TokenHours"], out var hours) ? hours : 12);

    public string CreateToken(UserAccount user, string role, IReadOnlyCollection<string> permissions)
    {
        var payload = new TokenPayload(
            user.Id,
            user.EmployeeId,
            user.Email,
            role,
            permissions.ToArray(),
            DateTimeOffset.UtcNow.Add(_lifetime).ToUnixTimeSeconds(),
            _issuer,
            _audience);

        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, JsonOptions)));
        var signature = Sign(body);
        return $"{body}.{signature}";
    }

    public bool TryValidate(string token, out UserContext? user)
    {
        user = null;
        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var body = parts[0];
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(Sign(body)),
                Encoding.UTF8.GetBytes(parts[1])))
        {
            return false;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(body));
            var payload = JsonSerializer.Deserialize<TokenPayload>(json, JsonOptions);
            if (payload is null)
            {
                return false;
            }

            if (payload.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            if (!string.Equals(payload.Issuer, _issuer, StringComparison.Ordinal) ||
                !string.Equals(payload.Audience, _audience, StringComparison.Ordinal))
            {
                return false;
            }

            user = new UserContext(
                payload.UserId,
                payload.EmployeeId,
                payload.Email,
                payload.Role,
                payload.Permissions);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string Sign(string body)
    {
        using var hmac = new HMACSHA256(_key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(body)));
    }
}
