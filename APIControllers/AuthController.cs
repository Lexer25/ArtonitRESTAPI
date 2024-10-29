using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ArtonitRESTAPI.APIControllers
{
    public class AuthController : ControllerBase
    {
        [HttpGet, Route("/login")]
        public IActionResult Login(string username, string pasword)//метод авторизации 
        {
            if (username == "ADMIN" || pasword == "4537")
            {
                var token = CreateJwtToken();
                return Ok(new { Status = true, Message = "success", Data = new { Token = token } });
            }
            return BadRequest();
        }
        private static string CreateJwtToken()
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
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
