using ChessVR.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ChessVR.Editor
{
    public static class GameUIBuilder
    {
        private const string CanvasName = "ChessGameCanvas";
        private const string LegacyCanvasName = "ChessGameOverCanvas";
        private const string RootPanelName = "ChessStudioPanel";
        private const string PanelName = "GameOverPanel";
        private const string TextName = "GameOverText";
        private const string ButtonName = "PlayAgainButton";
        private static readonly Vector3 CanvasLocalPosition = new(-3.22f, 0.525f, 0f);
        private static readonly Quaternion CanvasLocalRotation = Quaternion.Euler(0f, -90f, 0f);
        private const float CanvasLocalScale = 0.0015f;

        [MenuItem("ChessVR/Generate Chess UI")]
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

            BuildChessUIForSandbox(sandboxRoot, selectPanel: true, showDialog: true);
        }

        public static GameUIManager BuildChessUIForSandbox(GameObject sandboxRoot, bool selectPanel, bool showDialog)
        {
            if (sandboxRoot == null)
            {
                return null;
            }

            var boardPresenter = sandboxRoot.GetComponent<BoardPresenter>();
            var gameController = sandboxRoot.GetComponent<ChessGameController>();
            var audioManager = Object.FindFirstObjectByType<GameAudioManager>();

            RemoveExistingCanvas(sandboxRoot.transform);
            EnsureEventSystem();

            var canvas = BuildCanvas(sandboxRoot.transform);
            var rootPanel = BuildRootPanel(canvas.transform);
            var turnStatusText = BuildText(rootPanel.transform, "TurnStatusText", "Ruch: Biale", 34f, FontStyles.Bold);
            SetRect(turnStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -102f), new Vector2(-36f, -36f));

            var stateStatusText = BuildText(rootPanel.transform, "StateStatusText", "Partia trwa", 24f, FontStyles.Normal);
            stateStatusText.color = new Color(0.78f, 0.72f, 0.62f, 1f);
            SetRect(stateStatusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(36f, -152f), new Vector2(-36f, -106f));

            var restartButton = BuildButton(
                rootPanel.transform,
                "RestartButton",
                "Restart",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(36f, -232f),
                new Vector2(270f, -168f),
                new Color(0.23f, 0.42f, 0.33f, 0.96f));

            BuildSectionLabel(rootPanel.transform, "AudioLabel", "Audio", new Vector2(36f, -286f), new Vector2(270f, -246f));
            BuildSectionLabel(rootPanel.transform, "MusicLabel", "Music", new Vector2(36f, -344f), new Vector2(156f, -306f));
            var musicSlider = BuildSlider(rootPanel.transform, "MusicSlider", new Vector2(168f, -342f), new Vector2(516f, -306f), 0.25f);

            BuildSectionLabel(rootPanel.transform, "SfxLabel", "SFX", new Vector2(36f, -400f), new Vector2(156f, -362f));
            var sfxSlider = BuildSlider(rootPanel.transform, "SfxSlider", new Vector2(168f, -398f), new Vector2(516f, -362f), 0.8f);
            var muteToggle = BuildToggle(rootPanel.transform, "MuteToggle", "Mute", new Vector2(36f, -468f), new Vector2(260f, -420f));

            var gameOverPanel = BuildGameOverPanel(rootPanel.transform);
            var gameOverText = BuildGameOverText(gameOverPanel.transform);
            var playAgainButton = BuildPlayAgainButton(gameOverPanel.transform);

            var uiManager = sandboxRoot.GetComponent<GameUIManager>()
                            ?? sandboxRoot.AddComponent<GameUIManager>();

            WireGameUIManager(
                uiManager,
                gameOverPanel,
                gameOverText,
                playAgainButton,
                turnStatusText,
                stateStatusText,
                restartButton,
                audioManager,
                musicSlider,
                sfxSlider,
                muteToggle,
                gameController,
                boardPresenter);
            WireBoardPresenter(boardPresenter, uiManager);

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            if (selectPanel)
            {
                Selection.activeGameObject = rootPanel;
            }

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Chess UI Builder",
                    "Gotowe! World-space ChessGameCanvas jest zaznaczony w Hierarchy.\n" +
                    "Zapisz scenę (Ctrl+S), żeby zachować zmiany.",
                    "OK");
            }

            return uiManager;
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        private static void RemoveExistingCanvas(Transform sandboxRoot)
        {
            foreach (var name in new[] { CanvasName, LegacyCanvasName })
            {
                var existing = sandboxRoot != null ? sandboxRoot.Find(name)?.gameObject : null;
                if (existing == null)
                {
                    existing = GameObject.Find(name);
                }

                if (existing != null)
                {
                    Object.DestroyImmediate(existing);
                }
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

        private static Canvas BuildCanvas(Transform parent)
        {
            var canvasGO = new GameObject(CanvasName);
            canvasGO.transform.SetParent(parent, false);
            canvasGO.transform.localPosition = CanvasLocalPosition;
            canvasGO.transform.localRotation = CanvasLocalRotation;
            canvasGO.transform.localScale = Vector3.one * CanvasLocalScale;

            var rect = canvasGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 520f);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            canvas.worldCamera = Camera.main;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
            return canvas;
        }

        private static GameObject BuildRootPanel(Transform parent)
        {
            var go = new GameObject(RootPanelName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.055f, 0.052f, 0.047f, 0.88f);

            return go;
        }

        private static GameObject BuildGameOverPanel(Transform parent)
        {
            var go = new GameObject(PanelName);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -150f), new Vector2(330f, 150f));

            var img = go.AddComponent<Image>();
            img.color = new Color(0.02f, 0.019f, 0.017f, 0.94f);

            go.SetActive(false);
            return go;
        }

        private static TextMeshProUGUI BuildGameOverText(Transform parent)
        {
            var tmp = BuildText(parent, TextName, "Koniec gry!", 44f, FontStyles.Bold);
            SetRect(tmp.rectTransform, new Vector2(0f, 0.35f), new Vector2(1f, 1f), new Vector2(28f, 0f), new Vector2(-28f, -24f));
            tmp.text = "Koniec gry!";
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button BuildPlayAgainButton(Transform parent)
        {
            return BuildButton(
                parent,
                ButtonName,
                "Zagraj ponownie",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-170f, 36f),
                new Vector2(170f, 104f),
                new Color(0.23f, 0.42f, 0.33f, 1f));
        }

        private static TextMeshProUGUI BuildText(Transform parent, string name, string text, float fontSize, FontStyles style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = new Color(0.94f, 0.91f, 0.84f, 1f);
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button BuildButton(
            Transform parent,
            string name,
            string labelText,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);

            var img = go.AddComponent<Image>();
            img.color = color;

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
            label.text = labelText;
            label.fontSize = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.96f, 0.94f, 0.88f, 1f);
            label.raycastTarget = false;

            return btn;
        }

        private static void BuildSectionLabel(Transform parent, string name, string text, Vector2 offsetMin, Vector2 offsetMax)
        {
            var label = BuildText(parent, name, text, 22f, FontStyles.Bold);
            SetRect(label.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), offsetMin, offsetMax);
        }

        private static Slider BuildSlider(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, float value)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), offsetMin, offsetMax);

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, false);
            var backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.16f, 0.14f, 0.11f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.62f, 0.48f, 0.28f, 1f);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(12f, 0f);
            handleAreaRect.offsetMax = new Vector2(-12f, 0f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28f, 28f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.94f, 0.91f, 0.84f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Toggle BuildToggle(Transform parent, string name, string label, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            SetRect(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), offsetMin, offsetMax);

            var box = new GameObject("Background");
            box.transform.SetParent(go.transform, false);
            var boxRect = box.AddComponent<RectTransform>();
            SetRect(boxRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, -16f), new Vector2(32f, 16f));
            var boxImage = box.AddComponent<Image>();
            boxImage.color = new Color(0.16f, 0.14f, 0.11f, 1f);

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(box.transform, false);
            var checkmarkRect = checkmark.AddComponent<RectTransform>();
            checkmarkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkmarkRect.sizeDelta = new Vector2(20f, 20f);
            var checkmarkImage = checkmark.AddComponent<Image>();
            checkmarkImage.color = new Color(0.62f, 0.48f, 0.28f, 1f);

            var labelText = BuildText(go.transform, "Label", label, 22f, FontStyles.Normal);
            SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, new Vector2(46f, 0f), Vector2.zero);

            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkmarkImage;
            return toggle;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void WireGameUIManager(
            GameUIManager uiManager,
            GameObject panel,
            TextMeshProUGUI text,
            Button button,
            TextMeshProUGUI turnStatusText,
            TextMeshProUGUI stateStatusText,
            Button restartButton,
            GameAudioManager audioManager,
            Slider musicSlider,
            Slider sfxSlider,
            Toggle muteToggle,
            ChessGameController gameController,
            BoardPresenter boardPresenter)
        {
            var so = new SerializedObject(uiManager);
            SetObjectReference(so, "gameOverPanel", panel);
            SetObjectReference(so, "gameOverText", text);
            SetObjectReference(so, "playAgainButton", button);
            SetObjectReference(so, "turnStatusText", turnStatusText);
            SetObjectReference(so, "stateStatusText", stateStatusText);
            SetObjectReference(so, "restartButton", restartButton);
            SetObjectReference(so, "audioManager", audioManager);
            SetObjectReference(so, "musicSlider", musicSlider);
            SetObjectReference(so, "sfxSlider", sfxSlider);
            SetObjectReference(so, "muteToggle", muteToggle);
            SetObjectReference(so, "gameController", gameController);
            SetObjectReference(so, "boardPresenter", boardPresenter);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireBoardPresenter(BoardPresenter boardPresenter, GameUIManager uiManager)
        {
            if (boardPresenter == null)
            {
                return;
            }

            var so = new SerializedObject(boardPresenter);
            SetObjectReference(so, "gameUIManager", uiManager);
            SetObjectReference(so, "gameAudioManager", Object.FindFirstObjectByType<GameAudioManager>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
