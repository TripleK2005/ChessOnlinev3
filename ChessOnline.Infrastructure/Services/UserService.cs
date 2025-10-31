using ChessOnline.Application.DTOs.Auth;
using ChessOnline.Application.Interfaces;
using ChessOnline.Domain.Entities;
using ChessOnline.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;

        public UserService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        public async Task<AuthResultDto> RegisterAsync(RegisterDto model)
        {
            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                NickName = model.NickName,
                EloRating = 1200
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                return new AuthResultDto
                {
                    Succeeded = false,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return new AuthResultDto
            {
                Succeeded = true,
                UserId = user.Id,
                UserName = user.UserName,
                NickName = user.NickName
            };
        }

        public async Task<AuthResultDto> LoginAsync(LoginDto model)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (!result.Succeeded)
                return new AuthResultDto { Succeeded = false, Errors = new[] { "Đăng nhập thất bại." } };

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (user == null) return new AuthResultDto { Succeeded = false };

            return new AuthResultDto
            {
                Succeeded = true,
                UserId = user.Id,
                UserName = user.UserName,
                NickName = user.NickName
            };
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<UserProfileDto?> GetMyProfileAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                NickName = user.NickName,
                Email = user.Email!,
                EloRating = user.EloRating,
                Wins = user.Wins,
                Losses = user.Losses,
                Draws = user.Draws,
                WinRate = user.WinRate
            };
        }

        public async Task<UserProfileDto?> GetUserProfileByUsernameAsync(string username)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username);
            if (user == null) return null;
            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                NickName = user.NickName,
                Email = user.Email!,
                EloRating = user.EloRating,
                Wins = user.Wins,
                Losses = user.Losses,
                Draws = user.Draws,
                WinRate = user.WinRate
            };
        }
    }
}
