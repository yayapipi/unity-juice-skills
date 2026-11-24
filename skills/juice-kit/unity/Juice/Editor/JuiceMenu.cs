using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 選單 Tools ▸ Juice。
/// AI Agent 可以直接呼叫 JuiceMenu.AddDemoToScene()，不會跳出任何對話框。
/// </summary>
public static class JuiceMenu
{
    [MenuItem("Tools/Juice/Create Demo Scene")]
    public static void CreateDemoScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Juice] 請先停止 Play 再建立示範場景。");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        AddDemoToScene();
    }

    [MenuItem("Tools/Juice/Add Demo To Current Scene")]
    public static void AddDemoToScene()
    {
#if UNITY_2023_1_OR_NEWER
        JuiceDemo existing = Object.FindAnyObjectByType<JuiceDemo>();
#else
        JuiceDemo existing = Object.FindObjectOfType<JuiceDemo>();
#endif
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[Juice] 場景裡已經有 Juice Demo 了，按 Play 開始。");
            return;
        }
        GameObject go = new GameObject("Juice Demo");
        go.AddComponent<JuiceDemo>();
        Undo.RegisterCreatedObjectUndo(go, "Add Juice Demo");
        Selection.activeGameObject = go;
        Debug.Log("[Juice] 已加入 Juice Demo，按 Play 開始。用左邊的開關一項一項打開回饋。");
    }
}
