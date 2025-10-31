using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Application.DTOs.Auth
{
    public class AuthResultDto
    {
        public bool Succeeded { get; set; }
        public IEnumerable<string>? Errors { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? NickName { get; set; }
        public int EloRating { get; set; }

    }
}
