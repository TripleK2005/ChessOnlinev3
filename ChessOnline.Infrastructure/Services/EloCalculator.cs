using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessOnline.Infrastructure.Services
{
    public class EloCalculator
    {
        private const int KFactor = 32; // K-factor, có thể điều chỉnh

        public static (int newPlayer1Rating, int newPlayer2Rating) CalculateNewRatings(int player1Rating, int player2Rating, Domain.Enums.GameResult result)
        {
            double player1Score;
            double player2Score;

            switch (result)
            {
                case Domain.Enums.GameResult.WhiteWins: // Giả sử Player1 là Trắng
                    player1Score = 1.0;
                    player2Score = 0.0;
                    break;
                case Domain.Enums.GameResult.BlackWins: // Giả sử Player2 là Đen
                    player1Score = 0.0;
                    player2Score = 1.0;
                    break;
                case Domain.Enums.GameResult.DrawByStalemate:
                case Domain.Enums.GameResult.DrawByRepetition:
                case Domain.Enums.GameResult.DrawByAgreement:
                case Domain.Enums.GameResult.DrawByInsufficientMaterial:
                    player1Score = 0.5;
                    player2Score = 0.5;
                    break;
                case Domain.Enums.GameResult.Aborted:
                case Domain.Enums.GameResult.Pending:
                    return (player1Rating, player2Rating);
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), "Kết quả trận đấu không hợp lệ để tính Elo.");
            }

            double expectedScore1 = 1.0 / (1.0 + Math.Pow(10, (double)(player2Rating - player1Rating) / 400.0));
            double expectedScore2 = 1.0 / (1.0 + Math.Pow(10, (double)(player1Rating - player2Rating) / 400.0));

            int newPlayer1Rating = (int)Math.Round(player1Rating + KFactor * (player1Score - expectedScore1));
            int newPlayer2Rating = (int)Math.Round(player2Rating + KFactor * (player2Score - expectedScore2));

            return (newPlayer1Rating, newPlayer2Rating);
        }
    }
}
