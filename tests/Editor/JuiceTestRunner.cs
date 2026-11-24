using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 用批次模式跑 Juice Kit 的自我測試：
///   Unity -batchmode -projectPath 專案 -executeMethod JuiceTestRunner.Run -logFile log.txt
/// 不要加 -quit，測試跑完會自己結束，結束碼 = 失敗的項目數。
/// </summary>
public static class JuiceTestRunner
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        JuiceMenu.AddDemoToScene();
        new GameObject("Juice Self Test").AddComponent<JuiceSelfTest>();
        EditorApplication.EnterPlaymode();
    }
}
