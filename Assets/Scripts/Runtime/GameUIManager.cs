using ChessVR.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChessVR.Runtime
{
    public sealed class GameUIManager : MonoBehaviour
    {
        [Header("Panel końca gry")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private Button playAgainButton;

        [Header("Panel statusu")]
        [SerializeField] private TMP_Text turnStatusText;
        [SerializeField] private TMP_Text stateStatusText;
        [SerializeField] private Button restartButton;

        [Header("Audio UI")]
        [SerializeField] private GameAudioManager audioManager;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle muteToggle;

        [Header("Referencje do resetu gry")]
        [SerializeField] private ChessGameController gameController;
        [SerializeField] private BoardPresenter boardPresenter;

        private bool _gameOverShown;

        private void Awake()
        {
            ResolveAudioManager();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnPlayAgainClicked);
            }

            WireAudioControls();
            UpdateGameStatus(gameController != null ? gameController.CurrentBoard : null);
        }

        private void OnDestroy()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnPlayAgainClicked);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(OnMuteToggleChanged);
            }
        }

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

            if (!_gameOverShown)
            {
                ResolveAudioManager();
                audioManager?.PlayGameOver();
                _gameOverShown = true;
            }
        }

        public void HideGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            _gameOverShown = false;
        }

        public void OnPlayAgainClicked()
        {
            HideGameOver();
            gameController?.ResetMatch();
            boardPresenter?.RebuildImmediate();
            UpdateGameStatus(gameController != null ? gameController.CurrentBoard : null);
        }

        internal void UpdateGameStatus(BoardState board, bool choosingPromotion = false)
        {
            if (turnStatusText == null && stateStatusText == null)
            {
                return;
            }

            if (board == null)
            {
                SetStatusText(string.Empty, string.Empty);
                return;
            }

            if (choosingPromotion)
            {
                var side = board.SideToMove == PieceColor.White ? "Biale" : "Czarne";
                SetStatusText($"Ruch: {side}", "Wybierz promocje pionka");
                return;
            }

            if (board.IsCheckmate(board.SideToMove))
            {
                var winner = board.SideToMove == PieceColor.White ? "Czarne" : "Biale";
                SetStatusText("Koniec gry", $"Szach-mat - wygrywaja {winner}");
                return;
            }

            if (board.IsStalemate(board.SideToMove))
            {
                SetStatusText("Koniec gry", "Pat - remis");
                return;
            }

            var sideToMove = board.SideToMove == PieceColor.White ? "Biale" : "Czarne";
            var state = board.IsInCheck(board.SideToMove) ? "Szach" : "Partia trwa";
            SetStatusText($"Ruch: {sideToMove}", state);
        }

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

        private void ResolveAudioManager()
        {
            if (audioManager != null)
            {
                return;
            }

            audioManager = GameAudioManager.Instance != null
                ? GameAudioManager.Instance
                : FindFirstObjectByType<GameAudioManager>();
        }

        private void WireAudioControls()
        {
            if (audioManager == null)
            {
                return;
            }

            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(audioManager.MusicVolume);
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(audioManager.SfxVolume);
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.SetIsOnWithoutNotify(audioManager.IsMuted);
                muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
            }
        }

        private void SetStatusText(string turn, string state)
        {
            if (turnStatusText != null)
            {
                turnStatusText.text = turn;
            }

            if (stateStatusText != null)
            {
                stateStatusText.text = state;
            }
        }

        private void OnMusicSliderChanged(float value)
        {
            ResolveAudioManager();
            audioManager?.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            ResolveAudioManager();
            audioManager?.SetSfxVolume(value);
        }

        private void OnMuteToggleChanged(bool value)
        {
            ResolveAudioManager();
            audioManager?.SetMuted(value);
        }
    }
}
