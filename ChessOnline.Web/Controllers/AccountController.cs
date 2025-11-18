using ChessOnline.Application.DTOs.Auth;
using ChessOnline.Infrastructure.Services;
using ChessOnline.Web.Models;
using Microsoft.AspNetCore.Mvc;
using ChessOnline.Application.Interfaces;

namespace ChessOnline.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Register() => View();
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid registration data." });

            var dto = new RegisterDto
            {
                UserName = model.UserName,
                Email = model.Email,
                NickName = model.NickName,
                Password = model.Password
            };

            var result = await _userService.RegisterAsync(dto);

            if (!result.Succeeded)
            {
                var msg = string.Join(", ", result.Errors ?? new[] { "Đăng ký thất bại" });
                return Json(new { success = false, message = msg });
            }

            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid login data." });

            var dto = new LoginDto
            {
                UserName = model.UserName,
                Password = model.Password,
                RememberMe = model.RememberMe
            };

            var result = await _userService.LoginAsync(dto);

            if (!result.Succeeded)
            {
                var msg = string.Join(", ", result.Errors ?? new[] { "Đăng nhập thất bại" });
                return Json(new { success = false, message = msg });
            }

            // Redirect to home page after successful login
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutAsync();
            return Json(new { success = true });
        }




       
    }
}
