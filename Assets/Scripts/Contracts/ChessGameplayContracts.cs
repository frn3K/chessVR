using System.Collections.Generic;
using ChessVR.Domain;

namespace ChessVR.Contracts
{
    public interface IChessRulesAdapter
    {
        IReadOnlyList<ChessMove> GetLegalMovesForSide(BoardState board, PieceColor side);

        IReadOnlyList<ChessMove> GetLegalMovesFrom(BoardState board, BoardSquare from);

        bool TryMakeMove(BoardState board, ChessMove move);
    }

    public sealed class BoardStateRulesAdapter : IChessRulesAdapter
    {
        public IReadOnlyList<ChessMove> GetLegalMovesForSide(BoardState board, PieceColor side)
        {
            return board.GetLegalMovesFor(side);
        }

        public IReadOnlyList<ChessMove> GetLegalMovesFrom(BoardState board, BoardSquare from)
        {
            return board.GetLegalMoves(from);
        }

        public bool TryMakeMove(BoardState board, ChessMove move)
        {
            return board.TryMakeMove(move);
        }
    }
}
