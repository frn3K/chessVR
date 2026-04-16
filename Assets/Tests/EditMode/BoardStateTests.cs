using System.Linq;
using ChessVR.Domain;
using NUnit.Framework;

namespace ChessVR.Domain.Tests
{
    public class BoardStateTests
    {
        [Test]
        public void InitialPosition_SetsUpExpectedBoard()
        {
            var board = BoardState.CreateInitial();

            Assert.That(board.SideToMove, Is.EqualTo(PieceColor.White));
            Assert.That(board.CountPieces(), Is.EqualTo(32));
            Assert.That(board.GetPiece(BoardSquare.Parse("e1")), Is.EqualTo(new Piece(PieceType.King, PieceColor.White)));
            Assert.That(board.GetPiece(BoardSquare.Parse("d8")), Is.EqualTo(new Piece(PieceType.Queen, PieceColor.Black)));
            Assert.That(board.GetPiece(BoardSquare.Parse("a2")), Is.EqualTo(new Piece(PieceType.Pawn, PieceColor.White)));
            Assert.That(board.GetPiece(BoardSquare.Parse("h7")), Is.EqualTo(new Piece(PieceType.Pawn, PieceColor.Black)));
        }

        [Test]
        public void InitialPosition_WhiteHasTwentyLegalMoves()
        {
            var board = BoardState.CreateInitial();

            var legalMoves = board.GetLegalMoves();

            Assert.That(legalMoves.Count, Is.EqualTo(20));
        }

        [Test]
        public void PawnDoublePush_SetsEnPassantTargetAndChangesTurn()
        {
            var board = BoardState.CreateInitial();

            var success = board.TryMakeMove(new ChessMove(BoardSquare.Parse("e2"), BoardSquare.Parse("e4")));

            Assert.That(success, Is.True);
            Assert.That(board.SideToMove, Is.EqualTo(PieceColor.Black));
            Assert.That(board.EnPassantTarget, Is.EqualTo(BoardSquare.Parse("e3")));
            Assert.That(board.GetPiece(BoardSquare.Parse("e4")), Is.EqualTo(new Piece(PieceType.Pawn, PieceColor.White)));
        }

        [Test]
        public void EnPassantCapture_RemovesThePassedPawn()
        {
            var board = BoardState.CreateInitial();

            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("e2"), BoardSquare.Parse("e4"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("a7"), BoardSquare.Parse("a6"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("e4"), BoardSquare.Parse("e5"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("d7"), BoardSquare.Parse("d5"))), Is.True);

            var success = board.TryMakeMove(new ChessMove(BoardSquare.Parse("e5"), BoardSquare.Parse("d6")));

            Assert.That(success, Is.True);
            Assert.That(board.GetPiece(BoardSquare.Parse("d6")), Is.EqualTo(new Piece(PieceType.Pawn, PieceColor.White)));
            Assert.That(board.GetPiece(BoardSquare.Parse("d5")).IsNone, Is.True);
        }

        [Test]
        public void KingsideCastling_IsLegalWhenPathIsClearAndSafe()
        {
            var board = BoardState.CreateEmpty();
            board.SetPiece(BoardSquare.Parse("e1"), new Piece(PieceType.King, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("h1"), new Piece(PieceType.Rook, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("e8"), new Piece(PieceType.King, PieceColor.Black));
            board.SetCastlingRights(CastlingRights.WhiteKingSide);

            var legalMoves = board.GetLegalMoves(BoardSquare.Parse("e1"));

            Assert.That(legalMoves, Does.Contain(new ChessMove(BoardSquare.Parse("e1"), BoardSquare.Parse("g1"))));
        }

        [Test]
        public void Promotion_GeneratesFourPromotionChoices()
        {
            var board = BoardState.CreateEmpty();
            board.SetPiece(BoardSquare.Parse("e1"), new Piece(PieceType.King, PieceColor.White));
            board.SetPiece(BoardSquare.Parse("e8"), new Piece(PieceType.King, PieceColor.Black));
            board.SetPiece(BoardSquare.Parse("a7"), new Piece(PieceType.Pawn, PieceColor.White));

            var moves = board.GetLegalMoves(BoardSquare.Parse("a7"));

            var promotions = moves
                .Where(move => move.To.Equals(BoardSquare.Parse("a8")))
                .Select(move => move.Promotion)
                .OrderBy(type => type)
                .ToArray();

            Assert.That(promotions, Is.EqualTo(new[]
            {
                PieceType.Knight,
                PieceType.Bishop,
                PieceType.Rook,
                PieceType.Queen
            }));
        }

        [Test]
        public void FoolsMate_IsDetectedAsCheckmate()
        {
            var board = BoardState.CreateInitial();

            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("f2"), BoardSquare.Parse("f3"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("e7"), BoardSquare.Parse("e5"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("g2"), BoardSquare.Parse("g4"))), Is.True);
            Assert.That(board.TryMakeMove(new ChessMove(BoardSquare.Parse("d8"), BoardSquare.Parse("h4"))), Is.True);

            Assert.That(board.IsCheckmate(PieceColor.White), Is.True);
        }
    }
}
