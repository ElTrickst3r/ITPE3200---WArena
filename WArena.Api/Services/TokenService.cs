using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WArena.Api.Models;

namespace WArena.Api.Services;

public interface ITokenService
{
    string CreateToken(ApplicationUser user,IEnumerable<string> roles);
}

public class TokenService(IConfiguration configuration) : ITokenService
{
    public string CreateToken(ApplicationUser user,IEnumerable<string> roles)
    {
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        var audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,user.Id),
            new(ClaimTypes.NameIdentifier,user.Id),
            new(ClaimTypes.Name,user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email,user.Email ?? string.Empty),
            new("username",user.UserName ?? user.DisplayName),
            new("displayName",user.UserName ?? user.DisplayName),
            new("security_stamp",user.SecurityStamp ?? string.Empty)
        };
        claims.AddRange(roles.Select(role=>new Claim(ClaimTypes.Role,role)));
        var credentials=new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),SecurityAlgorithms.HmacSha256);
        var token=new JwtSecurityToken(issuer,audience,claims,expires:DateTime.UtcNow.AddHours(8),signingCredentials:credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
