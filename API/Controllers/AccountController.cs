

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Dtos;
using API.Interfaces;
using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto register)
        {
            if (await UserExists(register.Username))
            {
                return BadRequest("Username already exists");
            }

            using var hmac = new HMACSHA512();

            var appUser = new AppUser
            {
                UserName = register.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(register.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(appUser);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                Username = appUser.UserName,
                Token = _tokenService.CreateToken(appUser)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto login)
        {
            var appUser = await _context.Users.SingleOrDefaultAsync(u => u.UserName.Equals(login.Username));

            if (appUser == null)
            {
                return Unauthorized("Invalid Username");
            }

            using var hmac = new HMACSHA512(appUser.PasswordSalt);

            var computerHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(login.Password));

            for (int i = 0; i < computerHash.Length; i++)
            {
                if (!computerHash[i].Equals(appUser.PasswordSalt))
                {
                    return Unauthorized("Invalid Password");
                }
            }

            return new UserDto
            {
                Username = appUser.UserName,
                Token = _tokenService.CreateToken(appUser)
            };
        }

        private async Task<bool> UserExists(string username) =>
            await _context.Users.AnyAsync(u => u.UserName.Equals(username.ToLower()));
    }
}