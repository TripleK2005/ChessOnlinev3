using ChessOnline.Application.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResultDto> RegisterAsync(RegisterDto model);
        Task<AuthResultDto> LoginAsync(LoginDto model);
        Task LogoutAsync();
        Task<UserProfileDto?> GetMyProfileAsync(string userId);
        Task<UserProfileDto?> GetUserProfileByUsernameAsync(string username);
    }
}
