using AuthService.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class TokenService(IConfiguration _configuration)
    {
        public string GenerateAccessToken(User user)
        {
            string? jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT ключ не найден в конфигурации.");
            }


            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "")
            };

            var token = new JwtSecurityToken(
                _configuration["Jwt:Issuer"],
                _configuration["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        /// Определяем, является ли запрос с Android-клиента
        public static bool IsRequestFromAndroid(HttpRequest request)
        {
            return request.Headers["X-Platform"].ToString().Trim().Equals("android", StringComparison.OrdinalIgnoreCase);
        }

        /// Устанавливаем refreshToken в зависимости от платформы (Web → Cookie, Android → Заголовок)
        public void SetRefreshToken(HttpResponse response, HttpRequest request, string refreshToken, DateTime expiry)
        {
            if (IsRequestFromAndroid(request))
            {
                response.Headers["X-Refresh-Token"] = refreshToken;
            }
            else
            {
                response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = expiry
                });
            }
        }

        /// Получаем refreshToken в зависимости от платформы (Web → Cookie, Android → заголовок)
        public string? GetRefreshToken(HttpRequest request)
        {
            var headerRefreshToken = request.Headers["X-Refresh-Token"].FirstOrDefault()?.Trim();
            if (!string.IsNullOrEmpty(headerRefreshToken))
            {
                return headerRefreshToken;
            }

            return request.Cookies.TryGetValue("refreshToken", out var refreshToken) ? refreshToken : null;
        }

        /// Удаляем refreshToken (Web → Cookie, Android → заголовок не удаляется)
        public void RemoveRefreshToken(HttpResponse response, HttpRequest request)
        {
            if (!IsRequestFromAndroid(request))
            {
                response.Cookies.Delete("refreshToken");
            }
        }

        public ClaimsPrincipal? ValidateAccessToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = _configuration["Jwt:Key"];
                if (string.IsNullOrEmpty(jwtKey))
                {
                    throw new InvalidOperationException("JWT ключ не найден в конфигурации.");
                }

                var key = Encoding.UTF8.GetBytes(jwtKey);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],

                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return principal;
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
            {
                return null;
            }
        }

    }
}
