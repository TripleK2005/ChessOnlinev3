using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Domain.Enums
{
    public enum FriendshipStatus
    {
        Pending,   // Đang chờ xác nhận
        Accepted,  // Đã là bạn bè
        Declined,  // Từ chối
        Blocked    // Bị chặn
    }
}
