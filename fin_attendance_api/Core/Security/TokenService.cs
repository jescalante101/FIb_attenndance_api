using Entities.Manager;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FibAttendanceApi.Data; // Added for ApplicationDbcontext
using Microsoft.EntityFrameworkCore; // Added for Include and ToListAsync

namespace FibAttendanceApi.Core.Security
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly ApplicationDbcontext _context; // Added DbContext

        public TokenService(IConfiguration config, ApplicationDbcontext context) // Modified constructor
        {
            _config = config;
            _context = context; // Assigned DbContext
        }

        public string CreateToken(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            // Fetch user permissions and add them as claims
            var userPermissions = _context.UserPermissions
                .Where(up => up.UserId == user.UserId)
                .Include(up => up.Permission)
                .Select(up => up.Permission.PermissionKey)
                .ToList();

            foreach (var permissionKey in userPermissions)
            {
                claims.Add(new Claim(ClaimTypes.Role, permissionKey)); // Using ClaimTypes.Role for permissions
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7),
                SigningCredentials = creds,
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}