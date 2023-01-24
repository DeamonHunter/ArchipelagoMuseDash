using Assets.Scripts.PeroTools.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoMuseDash.Helpers {
    /// <summary>
    /// A collection of methods to help print out information about gameobjects in the heirachy
    /// </summary>
    public static class AssetHelpers {
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
    }
}
