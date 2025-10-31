using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Domain.Enums
{
    public enum GameResult
    {
        WhiteWins,            // Trắng thắng
        BlackWins,            // Đen thắng
        DrawByStalemate,      // Hòa do không có nước đi
        DrawByRepetition,     // Hòa do lặp lại thế cờ
        DrawByAgreement,      // Hòa do thỏa thuận
        DrawByInsufficientMaterial, // Hòa do thiếu lực lượng
        DrawByThreefoldRepetition, // Hòa do lặp lại ba lần
        DrawByFiftyMoveRule, // Hòa do quy tắc 50 nước
        Draw,                // Hòa chung chung
        Aborted,                // Trận đấu bị hủy
        Pending                 // Trận đấu đang chờ kết quả
    }
}
