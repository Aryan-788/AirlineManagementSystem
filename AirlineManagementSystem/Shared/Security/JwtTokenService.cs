using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Security;

public interface ITokenService
{
    string GenerateToken(int userId, string email, string role);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}

public class JwtTokenService : ITokenService
{
    private readonly string _key;

    private readonly string _issuer;
    private readonly IEnumerable<string> _audiences;
    private readonly int _expirationMinutes;

    public JwtTokenService(string key, string issuer, IEnumerable<string> audiences, int expirationMinutes)
    {
        _key = key;
        _issuer = issuer;
        _audiences = audiences;
        _expirationMinutes = expirationMinutes;
    }

    public string GenerateToken(int userId, string email, string role)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
            Issuer = _issuer,
            SigningCredentials = credentials
        };

        // Add multiple audiences
        foreach (var audience in _audiences)
        {
            tokenDescriptor.Audience = audience; // Note: This technically sets one, but we manually add multiple later or use claims if needed.
            // A better way with JwtSecurityToken:
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audiences.FirstOrDefault(), // Primary audience
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expirationMinutes),
            signingCredentials: credentials
        );

        // Add additional audiences as claims if there are more than one
        if (_audiences.Count() > 1)
        {
            foreach (var extraAudience in _audiences.Skip(1))
            {
                token.Payload.AddClaim(new Claim(JwtRegisteredClaimNames.Aud, extraAudience));
            }
        }

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_key));

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }
}
