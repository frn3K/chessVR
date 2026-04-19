using System.Collections.Generic;
using ChessVR.Domain;

namespace ChessVR.Contracts
{
    /// <summary>
    /// Granica między „silnikiem zasad szachów” a resztą gry (VR, UI, AI).
    /// Dziś i tak używamy <see cref="BoardState"/> — adapter tylko przekazuje wywołania 1:1.
    /// Później można podmienić implementację na bibliotekę albo inny silnik bez przepisywania sceny.
    /// </summary>
    public interface IChessRulesAdapter
    {
        IReadOnlyList<ChessMove> GetLegalMovesForSide(BoardState board, PieceColor side);

        IReadOnlyList<ChessMove> GetLegalMovesFrom(BoardState board, BoardSquare from);

        bool TryMakeMove(BoardState board, ChessMove move);
    }

    /// <summary>
    /// Coś, co wybiera ruch przeciwnika (np. prosty bot albo Stockfish za adapterem).
    /// Na dziś wystarczy sam interfejs — pierwsza implementacja przyjdzie z zadaniem „prosty AI provider”.
    /// </summary>
    public interface IAiMoveProvider
    {
        bool TrySelectMove(BoardState board, IReadOnlyList<ChessMove> legalMoves, out ChessMove chosenMove);
    }

    /// <summary>
    /// Domyślny adapter: cała prawda o szachach zostaje w <see cref="BoardState"/>.
    /// </summary>
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
