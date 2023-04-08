using Il2CppAssets.Scripts.PeroTools.Managers;
using MelonLoader;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoMuseDash.Helpers;

/// <summary>
/// A collection of methods to help print out information about gameobjects in the heirachy
/// </summary>
public static class AssetHelpers {
    public static MelonAssembly Assembly;

    public static void PrintoutAllGameObjects() {
        var list = new Il2CppSystem.Collections.Generic.List<GameObject>();
        SceneManager.instance.curScene.GetRootGameObjects(list);
        foreach (var gameobject in list)
            RecursiveGameObjectPrintout(gameobject, 0, true);
    }

    public static void RecursiveGameObjectPrintout(GameObject go, int depth, bool showName) {
        if (showName) {
            ArchipelagoStatic.ArchLogger.Log("Scene Load", $"{new string('-', depth)} {go.name}");
            var list = new Il2CppSystem.Collections.Generic.List<MonoBehaviour>();
            go.GetComponents(list);

            foreach (var behaviour in list) {
                if (behaviour == null)
                    continue;

                ArchipelagoStatic.ArchLogger.Log("Scene Load", $"[{behaviour.GetScriptClassName()}]");
            }
        }

        for (int i = 0; i < go.transform.childCount; i++) {
            var child = go.transform.GetChild(i);
            if (child.gameObject == null)
                continue;

            RecursiveGameObjectPrintout(child.gameObject, depth + 1, showName);
        }
    }

    public static void CopyTextVariables(Text copyFrom, Text copyTo) {
        copyTo.font = copyFrom.font;
        copyTo.fontStyle = copyFrom.fontStyle;
        copyTo.fontSize = copyFrom.fontSize;
        copyTo.alignment = copyFrom.alignment;
        copyTo.color = copyFrom.color;
    }

    public static Texture2D LoadTexture(string resourcePath) {
        using (var stream = Assembly.Assembly.GetManifestResourceStream(resourcePath)) {
            if (stream == null) {
                ArchipelagoStatic.ArchLogger.Warning("LoadExternalAssets", "Help");
                return null;
            }

            using (var ms = new MemoryStream()) {
                stream.CopyTo(ms);

                var archIconTexture = new Texture2D(1, 1);
                archIconTexture.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                archIconTexture.wrapMode = TextureWrapMode.Clamp;

                ImageConversion.LoadImage(archIconTexture, ms.ToArray());
                return archIconTexture;
            }
        }
    }
}