
using Entities.Manager;

namespace FibAttendanceApi.Core.Security
{
    public interface ITokenService
    {
        string CreateToken(AppUser user);
    }
}
