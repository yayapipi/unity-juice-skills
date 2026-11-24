using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飄字：傷害數字、+1、「Perfect!」。往上飄、先放大再淡出，用完自動回收。
///     JuiceFloatingText.Spawn(hitPoint, "12");
///     JuiceFloatingText.Spawn(hitPoint, "36!", Color.yellow, 1.6f);   // 暴擊：換色、放大
/// 用 Unity 內建的 TextMesh，不需要 TextMeshPro。Web 版要顯示中文請先指定 JuiceAssets.Font。
/// 動畫用 unscaled time，頓幀時也會動。
/// </summary>
public static class JuiceFloatingText
{
    /// <summary>2D 的排序順序。</summary>
    public static int SortingOrder = 200;

    static readonly Stack<TextMesh> pool = new Stack<TextMesh>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        pool.Clear();
    }

    public static void Spawn(Vector3 position, string text)
    {
        Spawn(position, text, Color.white, 1f, 0.8f, 1.2f);
    }

    /// <summary>size：1 大約是 0.6 個世界單位高；duration：整段動畫幾秒；rise：往上飄多遠。</summary>
    public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float duration = 0.8f, float rise = 1.2f)
    {
        if (!JuiceRunner.Available || string.IsNullOrEmpty(text)) return;
        TextMesh mesh = Get();
        if (mesh == null) return;
        mesh.text = text;
        mesh.color = color;
        mesh.gameObject.SetActive(true);
        JuiceRunner.Run(Animate(mesh, position, color, size, Mathf.Max(0.05f, duration), rise));
    }

    static TextMesh Get()
    {
        while (pool.Count > 0)
        {
            TextMesh pooled = pool.Pop();
            if (pooled != null) return pooled;
        }
        JuiceRunner runner = JuiceRunner.Instance;
        if (runner == null) return null;

        GameObject go = new GameObject("[Juice] Floating Text");
        go.transform.SetParent(runner.transform, false);
        TextMesh mesh = go.AddComponent<TextMesh>();
        Font font = JuiceAssets.Font;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = 0.1f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontStyle = FontStyle.Bold;
        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (font != null) renderer.sharedMaterial = font.material;
        renderer.sortingOrder = SortingOrder;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return mesh;
    }

    static IEnumerator Animate(TextMesh mesh, Vector3 start, Color color, float size, float duration, float rise)
    {
        Transform tr = mesh.transform;
        Camera cam = Camera.main;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        start += right * Random.Range(-0.2f, 0.2f) * size;

        float t = 0f;
        while (t < 1f)
        {
            if (mesh == null) yield break;
            t += Time.unscaledDeltaTime / duration;
            float clamped = Mathf.Clamp01(t);
            // 先彈大（前 25%），再慢慢上飄，最後 40% 淡出
            float pop = clamped < 0.25f ? JuiceEase.Evaluate(JuiceEaseType.OutBack, clamped / 0.25f) : 1f;
            float alpha = clamped < 0.6f ? 1f : 1f - (clamped - 0.6f) / 0.4f;
            tr.position = start + up * (rise * JuiceEase.Evaluate(JuiceEaseType.OutCubic, clamped));
            tr.localScale = Vector3.one * (size * Mathf.Max(0.01f, pop));
            if (cam != null) tr.rotation = cam.transform.rotation;
            mesh.color = new Color(color.r, color.g, color.b, color.a * alpha);
            yield return null;
        }
        if (mesh != null)
        {
            mesh.gameObject.SetActive(false);
            pool.Push(mesh);
        }
    }
}
