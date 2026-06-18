using System.Collections.Generic;
using ChessVR.Contracts;
using ChessVR.Domain;
using UnityEngine;

namespace ChessVR.Runtime
{
    public sealed class ChessGameController : MonoBehaviour
    {
        [SerializeField] private bool resetOnAwake = true;

        public BoardState CurrentBoard { get; private set; }

        public IChessRulesAdapter RulesAdapter { get; private set; }

        private void Awake()
        {
            RulesAdapter ??= new BoardStateRulesAdapter();

            if (resetOnAwake || CurrentBoard == null)
            {
                ResetMatch();
            }
        }

        public void ResetMatch()
        {
            RulesAdapter ??= new BoardStateRulesAdapter();
            CurrentBoard = BoardState.CreateInitial();
        }

        public IReadOnlyList<ChessMove> GetLegalMovesForCurrentSide()
        {
            return RulesAdapter.GetLegalMovesForSide(CurrentBoard, CurrentBoard.SideToMove);
        }

        public IReadOnlyList<ChessMove> GetLegalMovesForSide(PieceColor side)
        {
            return RulesAdapter.GetLegalMovesForSide(CurrentBoard, side);
        }

        public IReadOnlyList<ChessMove> GetLegalMovesFrom(BoardSquare from)
        {
            return RulesAdapter.GetLegalMovesFrom(CurrentBoard, from);
        }

        public bool TryMakeMove(ChessMove move)
        {
            return RulesAdapter.TryMakeMove(CurrentBoard, move);
        }
    }
}
