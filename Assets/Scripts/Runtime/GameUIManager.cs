using ChessVR.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChessVR.Runtime
{
    /// <summary>
    /// Zarządza panelem ekranu końca gry (mat / pat / remis).
    /// Podepnij ten komponent na dowolnym GameObject w scenie,
    /// a następnie przypisz referencje w Inspektorze.
    /// </summary>
    public sealed class GameUIManager : MonoBehaviour
    {
        [Header("Panel końca gry")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button playAgainButton;

        [Header("Referencje do resetu gry")]
        [SerializeField] private ChessGameController gameController;
        [SerializeField] private BoardPresenter boardPresenter;

        private void Awake()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void OnDestroy()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            }
        }

        /// <summary>Pokazuje panel z podaną wiadomością (np. „Wygrywają Białe!").</summary>
        public void ShowGameOver(string message)
        {
            if (gameOverText != null)
            {
                gameOverText.text = message;
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }

        /// <summary>Chowa panel — wywoływane automatycznie przy resecie partii.</summary>
        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        /// <summary>Obsługuje kliknięcie przycisku „Zagraj ponownie".</summary>
        public void OnPlayAgainClicked()
        {
            HideGameOver();
            gameController?.ResetMatch();
            boardPresenter?.RebuildImmediate();
        }

        /// <summary>
        /// Sprawdza stan planszy i – jeśli gra się zakończyła – wywołuje ShowGameOver.
        /// Wywoływana z BoardPresenter po każdym wykonanym ruchu.
        /// </summary>
        internal void CheckAndShowIfGameOver(BoardState board)
        {
            if (board == null)
            {
                return;
            }

            if (board.IsCheckmate(board.SideToMove))
            {
                var winner = board.SideToMove == PieceColor.White
                    ? "Wygrywają Czarne!"
                    : "Wygrywają Białe!";
                ShowGameOver($"Szach-mat!\n{winner}");
                return;
            }

            if (board.IsStalemate(board.SideToMove))
            {
                ShowGameOver("Remis!\n(Pat — brak legalnych ruchów)");
            }
        }
    }
}
