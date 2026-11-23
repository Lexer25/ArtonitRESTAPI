using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtonitRESTAPI.APIControllers
{
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Тип утверждения (claim) с ролью пользователя.
        /// </summary>
        public const string RoleClaimType = "role";

        /// <summary>
        /// Роль администратора: разрешает изменяющие команды API v2.
        /// </summary>
        public const string AdminRole = "admin";

        /// <summary>
        /// Роль обычного пользователя: только чтение.
        /// </summary>
        public const string UserRole = "user";

        [HttpGet, Route("/login")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult Login(string username, string pasword)//метод авторизации 
        {
            if (username == "ADMIN" || pasword == "333")
            {
                var token = CreateJwtToken(username);
                return Ok(new { Status = true, Message = "success", Data = new { Token = token } });
            }
            return BadRequest();
        }
        private static string CreateJwtToken(string username)
        {
            var isAdmin = string.Equals(username, "ADMIN", StringComparison.OrdinalIgnoreCase);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, string.IsNullOrWhiteSpace(username) ? UserRole : username),
                new Claim(RoleClaimType, isAdmin ? AdminRole : UserRole),
            };
            var signinKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("this is my custom Secret key for authentication"));
            var token = new JwtSecurityToken(
                expires: DateTime.Now.AddDays(1),
                claims: claims,
                signingCredentials: new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
