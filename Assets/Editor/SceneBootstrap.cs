using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Codex.EditorTools
{
    public static class SceneBootstrap
    {
        private const string SandboxScenePath = "Assets/Scenes/Sandbox.unity";
        private const string XrOriginPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
        private const string SimulatorPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/XR Interaction Simulator/XR Interaction Simulator.prefab";
        private const string SimulatorUiPrefabPath = "Assets/Samples/XR Interaction Toolkit/3.3.1/XR Interaction Simulator/UI/XR Interaction Simulator UI.prefab";

        public static void CreateSandboxAndExit()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Sandbox";

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.transform.position = Vector3.zero;

            InstantiatePrefab(XrOriginPrefabPath, "XROrigin", Vector3.zero);
            InstantiatePrefab(SimulatorPrefabPath, "XRInteractionSimulator", Vector3.zero);
            InstantiatePrefab(SimulatorUiPrefabPath, "XRInteractionSimulatorUI", Vector3.zero);

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SandboxScenePath, true)
            };

            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }

        private static void InstantiatePrefab(string assetPath, string fallbackName, Vector3 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Could not load prefab at path: {assetPath}");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                Debug.LogWarning($"Could not instantiate prefab at path: {assetPath}");
                return;
            }

            instance.name = fallbackName;
            instance.transform.position = position;
        }
    }
}
