using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 閃白：把物件（含子物件）的圖瞬間換成純色，再換回來。2D Sprite 與 3D 模型都能用。
///     JuiceFlash.Play(gameObject);                    // 白色，0.06 秒
///     JuiceFlash.Play(gameObject, Color.red, 0.1f);   // 指定顏色與時間
/// 用 unscaled time，所以頓幀時也看得到。玩家在 JuiceSettings 關掉閃光後不會作用。
/// 需要 Assets/Juice/Resources 裡的兩個 Shader。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Juice/Juice Flash")]
public class JuiceFlash : MonoBehaviour
{
    [Tooltip("閃的顏色。受擊用白色，中毒、灼傷可以換色。")]
    public Color color = Color.white;

    [Tooltip("閃多久（秒）。0.03–0.06 大約是 2–4 格；超過 0.1 會變成「變色」而不是「閃」。")]
    public float duration = 0.06f;

    static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    static readonly List<Renderer> scratch = new List<Renderer>();
    static Shader spriteShader;
    static Shader meshShader;
    static bool warned;

    readonly List<Renderer> renderers = new List<Renderer>();
    readonly List<Material[]> originals = new List<Material[]>();
    Material spriteMaterial;
    Material meshMaterial;
    bool flashing;
    float until;
    int startFrame;

    public static void Play(GameObject target, float duration = 0.06f)
    {
        Play(target, Color.white, duration);
    }

    public static void Play(GameObject target, Color color, float duration = 0.06f)
    {
        if (target == null || !Application.isPlaying) return;
        JuiceFlash f = target.GetComponent<JuiceFlash>();
        if (f == null) f = target.AddComponent<JuiceFlash>();
        f.Flash(color, duration);
    }

    /// <summary>用 Inspector 上的顏色與時間閃一次。</summary>
    public void Flash()
    {
        Flash(color, duration);
    }

    public void Flash(Color flashColor, float seconds)
    {
        if (!JuiceSettings.Flashes || !isActiveAndEnabled) return;
        if (!EnsureMaterials()) return;

        spriteMaterial.SetColor(FlashColorId, flashColor);
        meshMaterial.SetColor(FlashColorId, flashColor);
        until = Time.unscaledTime + Mathf.Max(0.01f, seconds);
        startFrame = Time.frameCount;
        if (flashing) return;

        GetComponentsInChildren(false, scratch);
        for (int i = 0; i < scratch.Count; i++)
        {
            Renderer r = scratch[i];
            bool isSprite = r is SpriteRenderer;
            if (!isSprite && !(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
            if (r.GetComponent<TextMesh>() != null) continue;

            Material[] mats = r.sharedMaterials;
            Material[] swapped = new Material[Mathf.Max(1, mats.Length)];
            for (int m = 0; m < swapped.Length; m++) swapped[m] = isSprite ? spriteMaterial : meshMaterial;
            renderers.Add(r);
            originals.Add(mats);
            r.sharedMaterials = swapped;
        }
        scratch.Clear();
        flashing = renderers.Count > 0;
    }

    void Update()
    {
        // 至少畫過一格才換回來，不然幀率低時會完全看不到
        if (flashing && Time.unscaledTime >= until && Time.frameCount > startFrame) End();
    }

    void OnDisable()
    {
        if (flashing) End();
    }

    void OnDestroy()
    {
        if (spriteMaterial != null) Destroy(spriteMaterial);
        if (meshMaterial != null) Destroy(meshMaterial);
    }

    void End()
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) renderers[i].sharedMaterials = originals[i];
        }
        renderers.Clear();
        originals.Clear();
        flashing = false;
    }

    bool EnsureMaterials()
    {
        if (spriteMaterial != null && meshMaterial != null) return true;
        if (spriteShader == null) spriteShader = Resources.Load<Shader>("JuiceFlashSprite");
        if (meshShader == null) meshShader = Resources.Load<Shader>("JuiceFlashMesh");
        if (spriteShader == null || meshShader == null)
        {
            if (!warned)
            {
                warned = true;
                Debug.LogWarning("[Juice] 找不到閃白用的 Shader。請確認 Assets/Juice/Resources 裡有 JuiceFlashSprite.shader 與 JuiceFlashMesh.shader。");
            }
            return false;
        }
        if (spriteMaterial == null) spriteMaterial = new Material(spriteShader);
        if (meshMaterial == null) meshMaterial = new Material(meshShader);
        return true;
    }
}
