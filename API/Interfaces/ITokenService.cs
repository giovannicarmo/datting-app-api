using Domain;

namespace API.Interfaces
{
    public interface ITokenService
    {
        public string CreateToken(AppUser appUser);
    }
}