using ChessVR.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Codex.EditorTools
{
    public static class GameUIBuilder
    {
        private const string CanvasName = "ChessGameOverCanvas";
        private const string PanelName = "GameOverPanel";
        private const string TextName = "GameOverText";
        private const string ButtonName = "PlayAgainButton";

        [MenuItem("Tools/Generate Chess UI")]
        public static void GenerateChessUI()
        {
            var sandboxRoot = GameObject.Find("ChessSandboxRoot");
            if (sandboxRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Chess UI Builder",
                    "Nie znaleziono 'ChessSandboxRoot' na scenie.\n" +
                    "Upewnij się, że scena Sandbox jest otwarta.",
                    "OK");
                return;
            }

            var boardPresenter = sandboxRoot.GetComponent<BoardPresenter>();
            var gameController = sandboxRoot.GetComponent<ChessGameController>();

            RemoveExistingCanvas();
            EnsureEventSystem();

            var canvas = BuildCanvas();
            var panel = BuildPanel(canvas.transform);
            var gameOverText = BuildGameOverText(panel.transform);
            var playAgainButton = BuildPlayAgainButton(panel.transform);

            var uiManager = sandboxRoot.GetComponent<GameUIManager>()
                            ?? sandboxRoot.AddComponent<GameUIManager>();

            WireGameUIManager(uiManager, panel, gameOverText, playAgainButton, gameController, boardPresenter);
            WireBoardPresenter(boardPresenter, uiManager);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Selection.activeGameObject = panel;

            EditorUtility.DisplayDialog(
                "Chess UI Builder",
                "Gotowe! Panel GameOverPanel jest zaznaczony w Hierarchy.\n" +
                "Zapisz scenę (Ctrl+S), żeby zachować zmiany.",
                "OK");
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private static void RemoveExistingCanvas()
        {
            var existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static Canvas BuildCanvas()
        {
            var canvasGO = new GameObject(CanvasName);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject BuildPanel(Transform parent)
        {
            var go = new GameObject(PanelName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 440f);
            rect.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.05f, 0.88f);

            go.SetActive(false);
            return go;
        }

        private static TextMeshProUGUI BuildGameOverText(Transform parent)
        {
            var go = new GameObject(TextName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.4f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(24f, 0f);
            rect.offsetMax = new Vector2(-24f, -24f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "Koniec gry!";
            tmp.fontSize = 56f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;

            return tmp;
        }

        private static Button BuildPlayAgainButton(Transform parent)
        {
            var go = new GameObject(ButtonName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(360f, 84f);
            rect.anchoredPosition = new Vector2(0f, 48f);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.17f, 0.58f, 0.29f, 1f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Zagraj ponownie";
            label.fontSize = 34f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;

            return btn;
        }

        private static void WireGameUIManager(
            GameUIManager uiManager,
            GameObject panel,
            TextMeshProUGUI text,
            Button button,
            ChessGameController gameController,
            BoardPresenter boardPresenter)
        {
            var so = new SerializedObject(uiManager);
            so.FindProperty("gameOverPanel").objectReferenceValue = panel;
            so.FindProperty("gameOverText").objectReferenceValue = text;
            so.FindProperty("playAgainButton").objectReferenceValue = button;
            so.FindProperty("gameController").objectReferenceValue = gameController;
            so.FindProperty("boardPresenter").objectReferenceValue = boardPresenter;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireBoardPresenter(BoardPresenter boardPresenter, GameUIManager uiManager)
        {
            if (boardPresenter == null)
            {
                return;
            }

            var so = new SerializedObject(boardPresenter);
            so.FindProperty("gameUIManager").objectReferenceValue = uiManager;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
