using ChessVR.Domain;
using UnityEngine;

namespace ChessVR.Runtime
{
    public sealed class ChessGameController : MonoBehaviour
    {
        [SerializeField] private bool resetOnAwake = true;

        public BoardState CurrentBoard { get; private set; }

        private void Awake()
        {
            if (resetOnAwake || CurrentBoard == null)
            {
                ResetMatch();
            }
        }

        public void ResetMatch()
        {
            CurrentBoard = BoardState.CreateInitial();
        }
    }
}
