using System.Collections.Generic;
using ChessVR.Contracts;
using ChessVR.Domain;
using UnityEngine;

namespace ChessVR.Runtime
{
    public sealed class ChessGameController : MonoBehaviour
    {
        [SerializeField] private bool resetOnAwake = true;

        /// <summary>Stan partii — odczyt pozycji (np. dla prezentera). Zmiana zasad idzie przez <see cref="RulesAdapter"/>.</summary>
        public BoardState CurrentBoard { get; private set; }

        /// <summary>Silnik zasad; domyślnie <see cref="BoardStateRulesAdapter"/>.</summary>
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

        /// <summary>Legalne ruchy strony, która jest teraz na posunięciu.</summary>
        public IReadOnlyList<ChessMove> GetLegalMovesForCurrentSide()
        {
            return RulesAdapter.GetLegalMovesForSide(CurrentBoard, CurrentBoard.SideToMove);
        }

        /// <summary>Legalne ruchy dla wskazanego koloru (np. do analizy lub UI).</summary>
        public IReadOnlyList<ChessMove> GetLegalMovesForSide(PieceColor side)
        {
            return RulesAdapter.GetLegalMovesForSide(CurrentBoard, side);
        }

        /// <summary>Legalne ruchy figury stojącej na danym polu.</summary>
        public IReadOnlyList<ChessMove> GetLegalMovesFrom(BoardSquare from)
        {
            return RulesAdapter.GetLegalMovesFrom(CurrentBoard, from);
        }

        /// <summary>Wykonuje ruch, jeśli jest legalny — wyłącznie przez adapter zasad.</summary>
        public bool TryMakeMove(ChessMove move)
        {
            return RulesAdapter.TryMakeMove(CurrentBoard, move);
        }
    }
}
