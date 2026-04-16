using System;

namespace ChessVR.Domain
{
    public readonly struct Piece : IEquatable<Piece>
    {
        public static readonly Piece None = default;

        public PieceType Type { get; }
        public PieceColor Color { get; }
        public bool IsNone => Type == PieceType.None;

        public Piece(PieceType type, PieceColor color)
        {
            Type = type;
            Color = type == PieceType.None ? PieceColor.White : color;
        }

        public bool Equals(Piece other)
        {
            return Type == other.Type && Color == other.Color;
        }

        public override bool Equals(object obj)
        {
            return obj is Piece other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)Type, (int)Color);
        }

        public override string ToString()
        {
            if (IsNone)
            {
                return "--";
            }

            var colorPrefix = Color == PieceColor.White ? "W" : "B";
            return $"{colorPrefix}{Type}";
        }
    }
}
