using ChessVR.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Codex.EditorTools
{
    public static class MinimalStudioBuilder
    {
        private const string StudioRootName = "MinimalStudioRoot";
        private const string AudioManagerName = "GameAudioManager";

        [MenuItem("Tools/Generate Minimal Studio")]
        public static void GenerateMinimalStudio()
        {
            var sandboxRoot = GameObject.Find("ChessSandboxRoot");
            if (sandboxRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "Minimal Studio Builder",
                    "Nie znaleziono 'ChessSandboxRoot' na scenie.",
                    "OK");
                return;
            }

            BuildForSandbox(sandboxRoot);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Selection.activeGameObject = GameObject.Find(StudioRootName);
        }

        public static GameAudioManager BuildForSandbox(GameObject sandboxRoot)
        {
            if (sandboxRoot == null)
            {
                return null;
            }

            RemoveExistingStudio();
            RemoveLegacySceneObject("Ground");
            RemoveLegacySceneObject("Directional Light");

            var studioRoot = new GameObject(StudioRootName);
            studioRoot.transform.SetParent(sandboxRoot.transform, false);

            BuildFloor(studioRoot.transform);
            BuildBackdrop(studioRoot.transform);
            BuildLighting(studioRoot.transform);
            BuildVolume(studioRoot.transform);
            ConfigureRenderSettings();

            return EnsureAudioManager(sandboxRoot.transform);
        }

        public static GameAudioManager EnsureAudioManager(Transform sandboxRoot)
        {
            var existing = Object.FindFirstObjectByType<GameAudioManager>();
            var manager = existing;
            if (manager == null)
            {
                var go = new GameObject(AudioManagerName);
                go.transform.SetParent(sandboxRoot, false);
                manager = go.AddComponent<GameAudioManager>();
            }

            manager.gameObject.name = AudioManagerName;
            WireAudioClips(manager);
            return manager;
        }

        private static void RemoveExistingStudio()
        {
            var existing = GameObject.Find(StudioRootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void RemoveLegacySceneObject(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void BuildFloor(Transform parent)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "StudioFloor";
            floor.transform.SetParent(parent, false);
            floor.transform.localPosition = new Vector3(0f, -0.25f, -0.8f);
            floor.transform.localScale = new Vector3(7.5f, 0.5f, 7.5f);
            Tint(floor, new Color(0.32f, 0.28f, 0.24f, 1f));
        }

        private static void BuildBackdrop(Transform parent)
        {
            // All positions are LOCAL to sandboxRoot which sits at world (0, 0, 1.2).
            // Player (XR Origin) is at world (0, 0, 0) → local (0, 0, -1.2).
            // Board is on the floor (boardHeight=0.02). Room surrounds player + board.
            const float roomHalfWidth = 3.5f;
            const float roomFront = -2.8f;
            const float roomBack = 2.8f;
            const float roomHeight = 3.0f;
            const float wallThickness = 0.5f;
            var roomCenterZ = (roomFront + roomBack) * 0.5f;
            var roomDepth = roomBack - roomFront;

            var backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backWall.name = "BackWall";
            backWall.transform.SetParent(parent, false);
            backWall.transform.localPosition = new Vector3(0f, roomHeight * 0.5f, roomBack);
            backWall.transform.localScale = new Vector3(roomHalfWidth * 2f, roomHeight, wallThickness);
            Tint(backWall, new Color(0.45f, 0.38f, 0.32f, 1f));

            var leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(parent, false);
            leftWall.transform.localPosition = new Vector3(-roomHalfWidth, roomHeight * 0.5f, roomCenterZ);
            leftWall.transform.localScale = new Vector3(wallThickness, roomHeight, roomDepth);
            Tint(leftWall, new Color(0.35f, 0.30f, 0.25f, 1f));

            var rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(parent, false);
            rightWall.transform.localPosition = new Vector3(roomHalfWidth, roomHeight * 0.5f, roomCenterZ);
            rightWall.transform.localScale = new Vector3(wallThickness, roomHeight, roomDepth);
            Tint(rightWall, new Color(0.35f, 0.30f, 0.25f, 1f));

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(parent, false);
            ceiling.transform.localPosition = new Vector3(0f, roomHeight, roomCenterZ);
            ceiling.transform.localScale = new Vector3(roomHalfWidth * 2f, wallThickness, roomDepth);
            Tint(ceiling, new Color(0.22f, 0.20f, 0.18f, 1f));

            var frontWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontWall.name = "FrontWall";
            frontWall.transform.SetParent(parent, false);
            frontWall.transform.localPosition = new Vector3(0f, roomHeight * 0.5f, roomFront);
            frontWall.transform.localScale = new Vector3(roomHalfWidth * 2f, roomHeight, wallThickness);
            Tint(frontWall, new Color(0.35f, 0.30f, 0.25f, 1f));
        }


        private static void BuildLighting(Transform parent)
        {
            var key = new GameObject("WarmKeyLight");
            key.transform.SetParent(parent, false);
            key.transform.localPosition = new Vector3(-0.5f, 2.8f, -0.5f);
            key.transform.localRotation = Quaternion.Euler(65f, 30f, 0f);
            var keyLight = key.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = new Color(1f, 0.93f, 0.82f, 1f);
            keyLight.intensity = 1.4f;

            var boardSpot = new GameObject("BoardLight");
            boardSpot.transform.SetParent(parent, false);
            boardSpot.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            var boardLight = boardSpot.AddComponent<Light>();
            boardLight.type = LightType.Point;
            boardLight.color = new Color(1f, 0.95f, 0.88f, 1f);
            boardLight.intensity = 1.2f;
            boardLight.range = 6f;

            var fill = new GameObject("FillLight");
            fill.transform.SetParent(parent, false);
            fill.transform.localPosition = new Vector3(2f, 2.2f, -1.5f);
            var fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.85f, 0.90f, 1f, 1f);
            fillLight.intensity = 0.7f;
            fillLight.range = 7f;
        }

        private static void BuildVolume(Transform parent)
        {
            var volumeObject = new GameObject("StudioPostProcessVolume");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/DefaultVolumeProfile.asset");
        }

        private static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.42f, 0.38f, 1f);
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.10f, 0.09f, 1f);
            }
        }

        private static void WireAudioClips(GameAudioManager manager)
        {
            var so = new SerializedObject(manager);
            SetAudioClip(so, "musicLoop", "Assets/Audio/HeavenlyLoop.ogg");
            SetAudioClip(so, "pickupClip", "Assets/Audio/piece_pickup.ogg");
            SetAudioClip(so, "dropClip", "Assets/Audio/piece_drop.ogg");
            SetAudioClip(so, "captureClip", "Assets/Audio/piece_capture.ogg");
            SetAudioClip(so, "invalidClip", "Assets/Audio/piece_invalid.ogg");
            SetAudioClip(so, "gameOverClip", "Assets/Audio/game_over.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAudioClip(SerializedObject serializedObject, string propertyName, string assetPath)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            AssetDatabase.ImportAsset(assetPath);
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
        }

        private static void Tint(GameObject gameObject, Color color)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            var shader = ResolvePreferredShader();
            if (shader != null)
            {
                renderer.sharedMaterial = new Material(shader) { color = color };
            }
        }

        private static Shader ResolvePreferredShader()
        {
            var hasUrp = GraphicsSettings.currentRenderPipeline != null
                         || GraphicsSettings.defaultRenderPipeline != null;
            if (hasUrp)
            {
                var urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    return urpShader;
                }
            }

            return Shader.Find("Standard");
        }
    }
}
