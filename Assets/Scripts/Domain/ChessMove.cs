using System;

namespace ChessVR.Domain
{
    public readonly struct ChessMove : IEquatable<ChessMove>
    {
        public BoardSquare From { get; }
        public BoardSquare To { get; }
        public PieceType Promotion { get; }
        public bool IsPromotion => Promotion != PieceType.None;

        public ChessMove(BoardSquare from, BoardSquare to, PieceType promotion = PieceType.None)
        {
            From = from;
            To = to;
            Promotion = promotion;
        }

        public bool Equals(ChessMove other)
        {
            return From.Equals(other.From) && To.Equals(other.To) && Promotion == other.Promotion;
        }

        public override bool Equals(object obj)
        {
            return obj is ChessMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(From, To, (int)Promotion);
        }

        public override string ToString()
        {
            return IsPromotion
                ? $"{From}{To}{Promotion}"
                : $"{From}{To}";
        }
    }
}
