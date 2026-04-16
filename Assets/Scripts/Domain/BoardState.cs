using System;
using System.Collections.Generic;

namespace ChessVR.Domain
{
    public sealed class BoardState
    {
        private static readonly PieceType[] PromotionPieces =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };

        private static readonly (int file, int rank)[] KnightOffsets =
        {
            (-2, -1), (-2, 1), (-1, -2), (-1, 2),
            (1, -2), (1, 2), (2, -1), (2, 1)
        };

        private static readonly (int file, int rank)[] OrthogonalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        private static readonly (int file, int rank)[] DiagonalDirections =
        {
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        private static readonly (int file, int rank)[] QueenDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        };

        private readonly Piece[] _squares;

        public PieceColor SideToMove { get; private set; }
        public CastlingRights CastlingRights { get; private set; }
        public BoardSquare? EnPassantTarget { get; private set; }
        public int HalfmoveClock { get; private set; }
        public int FullmoveNumber { get; private set; }

        private BoardState(PieceColor sideToMove)
        {
            _squares = new Piece[64];
            SideToMove = sideToMove;
            CastlingRights = CastlingRights.None;
            EnPassantTarget = null;
            HalfmoveClock = 0;
            FullmoveNumber = 1;
        }

        private BoardState(
            Piece[] squares,
            PieceColor sideToMove,
            CastlingRights castlingRights,
            BoardSquare? enPassantTarget,
            int halfmoveClock,
            int fullmoveNumber)
        {
            _squares = squares;
            SideToMove = sideToMove;
            CastlingRights = castlingRights;
            EnPassantTarget = enPassantTarget;
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
        }

        public static BoardState CreateEmpty(PieceColor sideToMove = PieceColor.White)
        {
            return new BoardState(sideToMove);
        }

        public static BoardState CreateInitial()
        {
            var board = CreateEmpty(PieceColor.White);
            board.CastlingRights = CastlingRights.All;

            board.SetPiece(BoardSquare.Parse("a1"), new Piece(PieceType.Rook, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("b1"), new Piece(PieceType.Knight, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("c1"), new Piece(PieceType.Bishop, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("d1"), new Piece(PieceType.Queen, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("e1"), new Piece(PieceType.King, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("f1"), new Piece(PieceType.Bishop, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("g1"), new Piece(PieceType.Knight, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("h1"), new Piece(PieceType.Rook, PieceColor.White));

            board.SetPiece(BoardSquare.Parse("a8"), new Piece(PieceType.Rook, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("b8"), new Piece(PieceType.Knight, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("c8"), new Piece(PieceType.Bishop, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("d8"), new Piece(PieceType.Queen, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("e8"), new Piece(PieceType.King, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("f8"), new Piece(PieceType.Bishop, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("g8"), new Piece(PieceType.Knight, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("h8"), new Piece(PieceType.Rook, PieceColor.Black));

            for (var file = 0; file < 8; file++)
            {
                board.SetPiece(new BoardSquare(file, 1), new Piece(PieceType.Pawn, PieceColor.White));
                board.SetPiece(new BoardSquare(file, 6), new Piece(PieceType.Pawn, PieceColor.Black));
            }

            return board;
        }

        public BoardState Clone()
        {
            return new BoardState(
                (Piece[])_squares.Clone(),
                SideToMove,
                CastlingRights,
                EnPassantTarget,
                HalfmoveClock,
                FullmoveNumber);
        }

        public Piece GetPiece(BoardSquare square)
        {
            return _squares[square.Index];
        }

        public void SetPiece(BoardSquare square, Piece piece)
        {
            _squares[square.Index] = piece;
        }

        public void ClearSquare(BoardSquare square)
        {
            _squares[square.Index] = Piece.None;
        }

        public void SetSideToMove(PieceColor sideToMove)
        {
            SideToMove = sideToMove;
        }

        public void SetCastlingRights(CastlingRights castlingRights)
        {
            CastlingRights = castlingRights;
        }

        public void SetEnPassantTarget(BoardSquare? enPassantTarget)
        {
            EnPassantTarget = enPassantTarget;
        }

        public void SetMoveCounters(int halfmoveClock, int fullmoveNumber)
        {
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
        }

        public int CountPieces()
        {
            var count = 0;
            for (var index = 0; index < _squares.Length; index++)
            {
                if (!_squares[index].IsNone)
                {
                    count++;
                }
            }

            return count;
        }

        public List<ChessMove> GetLegalMoves()
        {
            return GetLegalMovesFor(SideToMove);
        }

        public List<ChessMove> GetLegalMoves(BoardSquare from)
        {
            var piece = GetPiece(from);
            if (piece.IsNone)
            {
                return new List<ChessMove>();
            }

            var result = new List<ChessMove>();
            var legalMoves = GetLegalMovesFor(piece.Color);
            for (var i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i].From.Equals(from))
                {
                    result.Add(legalMoves[i]);
                }
            }

            return result;
        }

        public List<ChessMove> GetLegalMovesFor(PieceColor color)
        {
            var pseudoMoves = new List<ChessMove>(128);
            for (var index = 0; index < _squares.Length; index++)
            {
                var piece = _squares[index];
                if (piece.IsNone || piece.Color != color)
                {
                    continue;
                }

                GeneratePseudoLegalMoves(BoardSquare.FromIndex(index), piece, pseudoMoves);
            }

            var legalMoves = new List<ChessMove>(pseudoMoves.Count);
            for (var i = 0; i < pseudoMoves.Count; i++)
            {
                var testBoard = Clone();
                testBoard.ApplyMoveUnchecked(pseudoMoves[i]);
                if (!testBoard.IsInCheck(color))
                {
                    legalMoves.Add(pseudoMoves[i]);
                }
            }

            return legalMoves;
        }

        public bool TryMakeMove(ChessMove requestedMove)
        {
            var legalMoves = GetLegalMoves();
            var normalizedMove = FindMatchingLegalMove(legalMoves, requestedMove);
            if (normalizedMove == null)
            {
                return false;
            }

            ApplyMoveUnchecked(normalizedMove.Value);
            return true;
        }

        public bool IsInCheck(PieceColor color)
        {
            if (!TryFindKing(color, out var kingSquare))
            {
                return false;
            }

            return IsSquareAttacked(kingSquare, OpponentOf(color));
        }

        public bool IsCheckmate(PieceColor color)
        {
            return IsInCheck(color) && GetLegalMovesFor(color).Count == 0;
        }

        public bool IsStalemate(PieceColor color)
        {
            return !IsInCheck(color) && GetLegalMovesFor(color).Count == 0;
        }

        private ChessMove? FindMatchingLegalMove(List<ChessMove> legalMoves, ChessMove requestedMove)
        {
            for (var i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i].Equals(requestedMove))
                {
                    return legalMoves[i];
                }
            }

            if (requestedMove.Promotion != PieceType.None)
            {
                return null;
            }

            for (var i = 0; i < legalMoves.Count; i++)
            {
                var legalMove = legalMoves[i];
                if (legalMove.From.Equals(requestedMove.From) &&
                    legalMove.To.Equals(requestedMove.To) &&
                    legalMove.Promotion == PieceType.Queen)
                {
                    return legalMove;
                }
            }

            return null;
        }

        private void ApplyMoveUnchecked(ChessMove move)
        {
            var movingPiece = GetPiece(move.From);
            var targetPiece = GetPiece(move.To);
            var isCapture = !targetPiece.IsNone;
            var isPawnMove = movingPiece.Type == PieceType.Pawn;

            ClearSquare(move.From);

            if (isPawnMove &&
                EnPassantTarget.HasValue &&
                move.To.Equals(EnPassantTarget.Value) &&
                move.From.File != move.To.File &&
                targetPiece.IsNone)
            {
                var captureRankOffset = movingPiece.Color == PieceColor.White ? -1 : 1;
                var capturedPawnSquare = new BoardSquare(move.To.File, move.To.Rank + captureRankOffset);
                ClearSquare(capturedPawnSquare);
                isCapture = true;
            }

            if (movingPiece.Type == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2)
            {
                MoveCastlingRook(move.From, move.To);
            }

            if (isPawnMove && IsPromotionRank(move.To, movingPiece.Color))
            {
                var promotionType = move.Promotion == PieceType.None ? PieceType.Queen : move.Promotion;
                movingPiece = new Piece(promotionType, movingPiece.Color);
            }

            SetPiece(move.To, movingPiece);
            UpdateCastlingRightsOnMove(move, movingPiece, targetPiece);
            UpdateMoveCounters(isPawnMove, isCapture);
            UpdateEnPassantTarget(move, movingPiece, isPawnMove);

            if (SideToMove == PieceColor.Black)
            {
                FullmoveNumber++;
            }

            SideToMove = OpponentOf(SideToMove);
        }

        private void UpdateMoveCounters(bool isPawnMove, bool isCapture)
        {
            HalfmoveClock = isPawnMove || isCapture ? 0 : HalfmoveClock + 1;
        }

        private void UpdateEnPassantTarget(ChessMove move, Piece movingPiece, bool isPawnMove)
        {
            EnPassantTarget = null;
            if (!isPawnMove)
            {
                return;
            }

            var distance = Math.Abs(move.To.Rank - move.From.Rank);
            if (distance == 2)
            {
                var middleRank = (move.From.Rank + move.To.Rank) / 2;
                EnPassantTarget = new BoardSquare(move.From.File, middleRank);
            }
        }

        private void UpdateCastlingRightsOnMove(ChessMove move, Piece movingPiece, Piece capturedPiece)
        {
            if (movingPiece.Type == PieceType.King)
            {
                if (movingPiece.Color == PieceColor.White)
                {
                    CastlingRights &= ~(CastlingRights.WhiteKingSide | CastlingRights.WhiteQueenSide);
                }
                else
                {
                    CastlingRights &= ~(CastlingRights.BlackKingSide | CastlingRights.BlackQueenSide);
                }
            }

            if (movingPiece.Type == PieceType.Rook)
            {
                RemoveRookCastlingRight(move.From);
            }

            if (capturedPiece.Type == PieceType.Rook)
            {
                RemoveRookCastlingRight(move.To);
            }
        }

        private void RemoveRookCastlingRight(BoardSquare rookSquare)
        {
            switch (rookSquare.ToString())
            {
                case "a1":
                    CastlingRights &= ~CastlingRights.WhiteQueenSide;
                    break;
                case "h1":
                    CastlingRights &= ~CastlingRights.WhiteKingSide;
                    break;
                case "a8":
                    CastlingRights &= ~CastlingRights.BlackQueenSide;
                    break;
                case "h8":
                    CastlingRights &= ~CastlingRights.BlackKingSide;
                    break;
            }
        }

        private void MoveCastlingRook(BoardSquare kingFrom, BoardSquare kingTo)
        {
            if (kingFrom.Equals(BoardSquare.Parse("e1")) && kingTo.Equals(BoardSquare.Parse("g1")))
            {
                MovePiece(BoardSquare.Parse("h1"), BoardSquare.Parse("f1"));
                return;
            }

            if (kingFrom.Equals(BoardSquare.Parse("e1")) && kingTo.Equals(BoardSquare.Parse("c1")))
            {
                MovePiece(BoardSquare.Parse("a1"), BoardSquare.Parse("d1"));
                return;
            }

            if (kingFrom.Equals(BoardSquare.Parse("e8")) && kingTo.Equals(BoardSquare.Parse("g8")))
            {
                MovePiece(BoardSquare.Parse("h8"), BoardSquare.Parse("f8"));
                return;
            }

            if (kingFrom.Equals(BoardSquare.Parse("e8")) && kingTo.Equals(BoardSquare.Parse("c8")))
            {
                MovePiece(BoardSquare.Parse("a8"), BoardSquare.Parse("d8"));
            }
        }

        private void MovePiece(BoardSquare from, BoardSquare to)
        {
            var piece = GetPiece(from);
            ClearSquare(from);
            SetPiece(to, piece);
        }

        private void GeneratePseudoLegalMoves(BoardSquare from, Piece piece, List<ChessMove> moves)
        {
            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GeneratePawnMoves(from, piece, moves);
                    break;
                case PieceType.Knight:
                    GenerateKnightMoves(from, piece, moves);
                    break;
                case PieceType.Bishop:
                    GenerateSlidingMoves(from, piece, moves, DiagonalDirections);
                    break;
                case PieceType.Rook:
                    GenerateSlidingMoves(from, piece, moves, OrthogonalDirections);
                    break;
                case PieceType.Queen:
                    GenerateSlidingMoves(from, piece, moves, QueenDirections);
                    break;
                case PieceType.King:
                    GenerateKingMoves(from, piece, moves);
                    break;
            }
        }

        private void GeneratePawnMoves(BoardSquare from, Piece piece, List<ChessMove> moves)
        {
            var forward = piece.Color == PieceColor.White ? 1 : -1;
            var startRank = piece.Color == PieceColor.White ? 1 : 6;

            if (from.TryOffset(0, forward, out var singleStep) && GetPiece(singleStep).IsNone)
            {
                AddPawnMove(from, singleStep, piece.Color, moves);

                if (from.Rank == startRank &&
                    singleStep.TryOffset(0, forward, out var doubleStep) &&
                    GetPiece(doubleStep).IsNone)
                {
                    moves.Add(new ChessMove(from, doubleStep));
                }
            }

            TryAddPawnCapture(from, piece, -1, forward, moves);
            TryAddPawnCapture(from, piece, 1, forward, moves);
        }

        private void TryAddPawnCapture(BoardSquare from, Piece piece, int fileOffset, int rankOffset, List<ChessMove> moves)
        {
            if (!from.TryOffset(fileOffset, rankOffset, out var target))
            {
                return;
            }

            var occupant = GetPiece(target);
            if (!occupant.IsNone && occupant.Color != piece.Color)
            {
                AddPawnMove(from, target, piece.Color, moves);
                return;
            }

            if (EnPassantTarget.HasValue && EnPassantTarget.Value.Equals(target))
            {
                moves.Add(new ChessMove(from, target));
            }
        }

        private void AddPawnMove(BoardSquare from, BoardSquare to, PieceColor color, List<ChessMove> moves)
        {
            if (IsPromotionRank(to, color))
            {
                for (var i = 0; i < PromotionPieces.Length; i++)
                {
                    moves.Add(new ChessMove(from, to, PromotionPieces[i]));
                }

                return;
            }

            moves.Add(new ChessMove(from, to));
        }

        private void GenerateKnightMoves(BoardSquare from, Piece piece, List<ChessMove> moves)
        {
            for (var i = 0; i < KnightOffsets.Length; i++)
            {
                var offset = KnightOffsets[i];
                if (from.TryOffset(offset.file, offset.rank, out var target) && CanOccupy(target, piece.Color))
                {
                    moves.Add(new ChessMove(from, target));
                }
            }
        }

        private void GenerateSlidingMoves(BoardSquare from, Piece piece, List<ChessMove> moves, (int file, int rank)[] directions)
        {
            for (var i = 0; i < directions.Length; i++)
            {
                var current = from;
                var direction = directions[i];

                while (current.TryOffset(direction.file, direction.rank, out var next))
                {
                    var occupant = GetPiece(next);
                    if (occupant.IsNone)
                    {
                        moves.Add(new ChessMove(from, next));
                        current = next;
                        continue;
                    }

                    if (occupant.Color != piece.Color)
                    {
                        moves.Add(new ChessMove(from, next));
                    }

                    break;
                }
            }
        }

        private void GenerateKingMoves(BoardSquare from, Piece piece, List<ChessMove> moves)
        {
            for (var fileDelta = -1; fileDelta <= 1; fileDelta++)
            {
                for (var rankDelta = -1; rankDelta <= 1; rankDelta++)
                {
                    if (fileDelta == 0 && rankDelta == 0)
                    {
                        continue;
                    }

                    if (from.TryOffset(fileDelta, rankDelta, out var target) && CanOccupy(target, piece.Color))
                    {
                        moves.Add(new ChessMove(from, target));
                    }
                }
            }

            GenerateCastlingMoves(from, piece, moves);
        }

        private void GenerateCastlingMoves(BoardSquare from, Piece piece, List<ChessMove> moves)
        {
            if (piece.Type != PieceType.King || IsInCheck(piece.Color))
            {
                return;
            }

            if (piece.Color == PieceColor.White && from.Equals(BoardSquare.Parse("e1")))
            {
                TryAddCastlingMove(
                    from,
                    BoardSquare.Parse("g1"),
                    BoardSquare.Parse("h1"),
                    new[] { BoardSquare.Parse("f1"), BoardSquare.Parse("g1") },
                    new[] { BoardSquare.Parse("f1"), BoardSquare.Parse("g1") },
                    CastlingRights.WhiteKingSide,
                    piece.Color,
                    moves);

                TryAddCastlingMove(
                    from,
                    BoardSquare.Parse("c1"),
                    BoardSquare.Parse("a1"),
                    new[] { BoardSquare.Parse("d1"), BoardSquare.Parse("c1"), BoardSquare.Parse("b1") },
                    new[] { BoardSquare.Parse("d1"), BoardSquare.Parse("c1") },
                    CastlingRights.WhiteQueenSide,
                    piece.Color,
                    moves);
            }

            if (piece.Color == PieceColor.Black && from.Equals(BoardSquare.Parse("e8")))
            {
                TryAddCastlingMove(
                    from,
                    BoardSquare.Parse("g8"),
                    BoardSquare.Parse("h8"),
                    new[] { BoardSquare.Parse("f8"), BoardSquare.Parse("g8") },
                    new[] { BoardSquare.Parse("f8"), BoardSquare.Parse("g8") },
                    CastlingRights.BlackKingSide,
                    piece.Color,
                    moves);

                TryAddCastlingMove(
                    from,
                    BoardSquare.Parse("c8"),
                    BoardSquare.Parse("a8"),
                    new[] { BoardSquare.Parse("d8"), BoardSquare.Parse("c8"), BoardSquare.Parse("b8") },
                    new[] { BoardSquare.Parse("d8"), BoardSquare.Parse("c8") },
                    CastlingRights.BlackQueenSide,
                    piece.Color,
                    moves);
            }
        }

        private void TryAddCastlingMove(
            BoardSquare kingFrom,
            BoardSquare kingTo,
            BoardSquare rookSquare,
            BoardSquare[] requiredEmptySquares,
            BoardSquare[] safeSquares,
            CastlingRights requiredRight,
            PieceColor kingColor,
            List<ChessMove> moves)
        {
            if ((CastlingRights & requiredRight) == 0)
            {
                return;
            }

            var rook = GetPiece(rookSquare);
            if (rook.Type != PieceType.Rook || rook.Color != kingColor)
            {
                return;
            }

            for (var i = 0; i < requiredEmptySquares.Length; i++)
            {
                if (!GetPiece(requiredEmptySquares[i]).IsNone)
                {
                    return;
                }
            }

            var attacker = OpponentOf(kingColor);
            for (var i = 0; i < safeSquares.Length; i++)
            {
                if (IsSquareAttacked(safeSquares[i], attacker))
                {
                    return;
                }
            }

            moves.Add(new ChessMove(kingFrom, kingTo));
        }

        private bool IsSquareAttacked(BoardSquare square, PieceColor attackerColor)
        {
            if (attackerColor == PieceColor.White)
            {
                if (HasPawnOn(square, attackerColor, -1, -1) || HasPawnOn(square, attackerColor, 1, -1))
                {
                    return true;
                }
            }
            else
            {
                if (HasPawnOn(square, attackerColor, -1, 1) || HasPawnOn(square, attackerColor, 1, 1))
                {
                    return true;
                }
            }

            for (var i = 0; i < KnightOffsets.Length; i++)
            {
                var offset = KnightOffsets[i];
                if (square.TryOffset(offset.file, offset.rank, out var source))
                {
                    var piece = GetPiece(source);
                    if (piece.Type == PieceType.Knight && piece.Color == attackerColor)
                    {
                        return true;
                    }
                }
            }

            for (var i = 0; i < DiagonalDirections.Length; i++)
            {
                if (RayHasAttacker(square, attackerColor, DiagonalDirections[i], PieceType.Bishop, PieceType.Queen))
                {
                    return true;
                }
            }

            for (var i = 0; i < OrthogonalDirections.Length; i++)
            {
                if (RayHasAttacker(square, attackerColor, OrthogonalDirections[i], PieceType.Rook, PieceType.Queen))
                {
                    return true;
                }
            }

            for (var fileDelta = -1; fileDelta <= 1; fileDelta++)
            {
                for (var rankDelta = -1; rankDelta <= 1; rankDelta++)
                {
                    if (fileDelta == 0 && rankDelta == 0)
                    {
                        continue;
                    }

                    if (square.TryOffset(fileDelta, rankDelta, out var source))
                    {
                        var piece = GetPiece(source);
                        if (piece.Type == PieceType.King && piece.Color == attackerColor)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool HasPawnOn(BoardSquare target, PieceColor color, int fileOffset, int rankOffset)
        {
            if (!target.TryOffset(fileOffset, rankOffset, out var source))
            {
                return false;
            }

            var piece = GetPiece(source);
            return piece.Type == PieceType.Pawn && piece.Color == color;
        }

        private bool RayHasAttacker(BoardSquare square, PieceColor attackerColor, (int file, int rank) direction, PieceType primary, PieceType secondary)
        {
            var current = square;
            while (current.TryOffset(direction.file, direction.rank, out var next))
            {
                var occupant = GetPiece(next);
                if (occupant.IsNone)
                {
                    current = next;
                    continue;
                }

                return occupant.Color == attackerColor &&
                       (occupant.Type == primary || occupant.Type == secondary);
            }

            return false;
        }

        private bool TryFindKing(PieceColor color, out BoardSquare kingSquare)
        {
            for (var index = 0; index < _squares.Length; index++)
            {
                var piece = _squares[index];
                if (piece.Type == PieceType.King && piece.Color == color)
                {
                    kingSquare = BoardSquare.FromIndex(index);
                    return true;
                }
            }

            kingSquare = default;
            return false;
        }

        private bool CanOccupy(BoardSquare square, PieceColor movingColor)
        {
            var occupant = GetPiece(square);
            return occupant.IsNone || occupant.Color != movingColor;
        }

        private static PieceColor OpponentOf(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        private static bool IsPromotionRank(BoardSquare square, PieceColor color)
        {
            return color == PieceColor.White ? square.Rank == 7 : square.Rank == 0;
        }
    }
}
