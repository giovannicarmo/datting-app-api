

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Dtos;
using Data;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        public AccountController(DataContext context) => _context = context;

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register(RegisterDto register)
        {
            if (await UserExists(register.Username))
            {
                return BadRequest("Username already exists");
            }

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = register.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(register.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AppUser>> Login(LoginDto login)
        {
            var user = await _context.Users.SingleOrDefaultAsync(u => u.UserName.Equals(login.Username));

            if (user == null)
            {
                return Unauthorized("Invalid Username");
            }

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computerHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(login.Password));

            for (int i = 0; i < computerHash.Length; i++)
            {
                if (!computerHash[i].Equals(user.PasswordSalt))
                {
                    return Unauthorized("Invalid Password");
                }
            }

            return user;
        }

        private async Task<bool> UserExists(string username) =>
            await _context.Users.AnyAsync(u => u.UserName.Equals(username.ToLower()));
    }
}