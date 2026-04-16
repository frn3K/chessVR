using System;

namespace ChessVR.Domain
{
    public readonly struct BoardSquare : IEquatable<BoardSquare>, IComparable<BoardSquare>
    {
        public int File { get; }
        public int Rank { get; }
        public int Index => (Rank * 8) + File;

        public BoardSquare(int file, int rank)
        {
            if (file < 0 || file > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(file), "File must be between 0 and 7.");
            }

            if (rank < 0 || rank > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), "Rank must be between 0 and 7.");
            }

            File = file;
            Rank = rank;
        }

        public static BoardSquare FromIndex(int index)
        {
            if (index < 0 || index > 63)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 0 and 63.");
            }

            return new BoardSquare(index % 8, index / 8);
        }

        public static BoardSquare Parse(string algebraic)
        {
            if (!TryParse(algebraic, out var square))
            {
                throw new ArgumentException($"Invalid square notation: {algebraic}", nameof(algebraic));
            }

            return square;
        }

        public static bool TryParse(string algebraic, out BoardSquare square)
        {
            square = default;

            if (string.IsNullOrWhiteSpace(algebraic) || algebraic.Length != 2)
            {
                return false;
            }

            var fileChar = char.ToLowerInvariant(algebraic[0]);
            var rankChar = algebraic[1];

            if (fileChar < 'a' || fileChar > 'h' || rankChar < '1' || rankChar > '8')
            {
                return false;
            }

            square = new BoardSquare(fileChar - 'a', rankChar - '1');
            return true;
        }

        public bool TryOffset(int fileDelta, int rankDelta, out BoardSquare square)
        {
            var nextFile = File + fileDelta;
            var nextRank = Rank + rankDelta;

            if (nextFile < 0 || nextFile > 7 || nextRank < 0 || nextRank > 7)
            {
                square = default;
                return false;
            }

            square = new BoardSquare(nextFile, nextRank);
            return true;
        }

        public int CompareTo(BoardSquare other)
        {
            return Index.CompareTo(other.Index);
        }

        public bool Equals(BoardSquare other)
        {
            return File == other.File && Rank == other.Rank;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardSquare other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(File, Rank);
        }

        public override string ToString()
        {
            return $"{(char)('a' + File)}{Rank + 1}";
        }
    }
}
