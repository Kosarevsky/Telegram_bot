using Services.Models;

namespace Services.Interfaces
{
    public interface IUserService
    {
        UserModel Get(long user);
    }
}
