using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChessOnline.Application.DTOs.Auth;
using FluentValidation;

namespace ChessOnline.Application.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("Tên đăng nhập không được để trống.");

            RuleFor(x => x.Email)
                .NotEmpty().EmailAddress().WithMessage("Email không hợp lệ.");

            RuleFor(x => x.NickName)
                .NotEmpty().WithMessage("Tên hiển thị không được để trống.");

            RuleFor(x => x.Password)
                 .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                 .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
                 .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
                 .Matches("[a-z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ thường.")
                 .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.")
                 .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.");
        }
    }
}
