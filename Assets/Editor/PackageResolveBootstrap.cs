using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace Codex.EditorTools
{
    public static class PackageResolveBootstrap
    {
        private static readonly string[] RequiredPackages =
        {
            "com.unity.inputsystem",
            "com.unity.render-pipelines.universal",
            "com.unity.ugui",
            "com.unity.xr.interaction.toolkit",
            "com.unity.xr.management",
            "com.unity.xr.openxr"
        };

        private static ListRequest _listRequest;
        private static DateTime _startedAt;

        public static void RunAndExit()
        {
            _startedAt = DateTime.UtcNow;
            _listRequest = Client.List(true);
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (_listRequest == null || !_listRequest.IsCompleted)
            {
                return;
            }

            EditorApplication.update -= Poll;

            if (_listRequest.Status == StatusCode.Failure)
            {
                UnityEngine.Debug.LogError($"Package resolution failed: {_listRequest.Error.message}");
                EditorApplication.Exit(1);
                return;
            }

            var installed = _listRequest.Result.ToDictionary(package => package.name, package => package.version);
            var missing = RequiredPackages.Where(packageName => !installed.ContainsKey(packageName)).ToArray();

            if (missing.Length > 0)
            {
                UnityEngine.Debug.LogError("Missing required packages after resolve: " + string.Join(", ", missing));
                EditorApplication.Exit(2);
                return;
            }

            foreach (var packageName in RequiredPackages)
            {
                UnityEngine.Debug.Log($"Resolved package: {packageName} @ {installed[packageName]}");
            }

            UnityEngine.Debug.Log($"Package resolve completed in {(DateTime.UtcNow - _startedAt).TotalSeconds:F1}s");
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
    }
}
