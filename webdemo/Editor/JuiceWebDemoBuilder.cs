using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 把 Juice Kit 的示範場景打包成網頁版（WebGL）。線上 Demo 就是用這支腳本做出來的。
///
/// 專案裡要先有 Assets/Juice（Juice Kit）與 Assets/WebGLTemplates/JuiceDemo（示範頁的範本）。
/// 命令列：
///   Unity -batchmode -projectPath 專案路徑 -buildTarget WebGL -executeMethod JuiceWebDemoBuilder.Build -quit -logFile build.log
/// 輸出位置由環境變數 JUICE_WEB_OUT 決定；沒有設定時是專案資料夾裡的 Builds/JuiceWebDemo。
/// 也可以在編輯器裡用選單 Tools ▸ Juice ▸ Build Web Demo。
/// </summary>
public static class JuiceWebDemoBuilder
{
    const string SceneFolder = "Assets/JuiceWebDemo";
    const string ScenePath = SceneFolder + "/JuiceWebDemo.unity";
    const string TemplateName = "JuiceDemo";

    [MenuItem("Tools/Juice/Build Web Demo")]
    public static void Build()
    {
        string output = Environment.GetEnvironmentVariable("JUICE_WEB_OUT");
        if (string.IsNullOrEmpty(output)) output = "Builds/JuiceWebDemo";

        bool ok = false;
        try
        {
            ok = Run(output);
        }
        catch (Exception e)
        {
            Debug.LogError("[JuiceWebDemo] " + e);
        }
        if (Application.isBatchMode) EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool Run(string output)
    {
        // 場景只有預設的鏡頭與燈光，加上一個 JuiceDemo 元件；其他東西都由程式在執行時產生
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        JuiceMenu.AddDemoToScene();
        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder("Assets", "JuiceWebDemo");
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            Debug.LogError("[JuiceWebDemo] 場景存檔失敗：" + ScenePath);
            return false;
        }

        PlayerSettings.productName = "Juice Kit Demo";
        PlayerSettings.companyName = "Yayapipi";
        PlayerSettings.bundleVersion = Juice.Version;
        PlayerSettings.runInBackground = true;          // 點到網頁其他地方時，示範不要停下來
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Medium);

        // GitHub Pages 不會替 .br 檔加上 Content-Encoding 標頭，所以把解壓縮程式一起打包進去
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.nameFilesAsHashes = false;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        if (Directory.Exists("Assets/WebGLTemplates/" + TemplateName))
            PlayerSettings.WebGL.template = "PROJECT:" + TemplateName;
        else
            Debug.LogWarning("[JuiceWebDemo] 找不到 Assets/WebGLTemplates/" + TemplateName + "，改用 Unity 內建的網頁範本。");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log("[JuiceWebDemo] result=" + summary.result + " errors=" + summary.totalErrors +
                  " size=" + (summary.totalSize / 1024) + "KB time=" + summary.totalTime + " output=" + Path.GetFullPath(output));
        return summary.result == BuildResult.Succeeded;
    }
}
