using UnityEngine;

/// <summary>
/// Juice Kit 共用的執行期資源：材質、貼圖、字型。全部用程式產生，不需要任何素材檔。
/// </summary>
public static class JuiceAssets
{
    static Material particleMaterial;
    static Material trailMaterial;
    static Texture2D circleTexture;
    static Sprite squareSprite;
    static Font font;
    static Sprite circleSprite;

    /// <summary>粒子用的材質（圓形、吃頂點顏色）。</summary>
    public static Material ParticleMaterial
    {
        get
        {
            if (particleMaterial == null)
            {
                particleMaterial = new Material(FindUnlitShader());
                particleMaterial.name = "Juice Particle";
                particleMaterial.mainTexture = CircleTexture;
            }
            return particleMaterial;
        }
    }

    /// <summary>拖尾用的材質（純色、吃頂點顏色）。</summary>
    public static Material TrailMaterial
    {
        get
        {
            if (trailMaterial == null)
            {
                trailMaterial = new Material(FindUnlitShader());
                trailMaterial.name = "Juice Trail";
            }
            return trailMaterial;
        }
    }

    /// <summary>32×32 的柔邊圓形。</summary>
    public static Texture2D CircleTexture
    {
        get
        {
            if (circleTexture == null)
            {
                const int size = 32;
                circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                circleTexture.name = "Juice Circle";
                circleTexture.wrapMode = TextureWrapMode.Clamp;
                Color32[] pixels = new Color32[size * size];
                float half = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                        float a = Mathf.Clamp01((1f - d) * 6f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                circleTexture.SetPixels32(pixels);
                circleTexture.Apply(false, true);
            }
            return circleTexture;
        }
    }

    /// <summary>直徑 1 單位的白色圓形 Sprite，眼睛、球與佔位圖用。</summary>
    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
            {
                const int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.name = "Juice Circle Sprite";
                tex.wrapMode = TextureWrapMode.Clamp;
                Color32[] pixels = new Color32[size * size];
                float half = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half));
                        float a = Mathf.Clamp01(half - d + 0.5f);
                        pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                    }
                }
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                circleSprite.name = "Juice Circle";
            }
            return circleSprite;
        }
    }

    /// <summary>1×1 單位的白色方塊 Sprite，示範場景與佔位圖用。</summary>
    public static Sprite SquareSprite
    {
        get
        {
            if (squareSprite == null)
            {
                Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                tex.name = "Juice Square";
                Color32[] pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(pixels);
                tex.Apply(false, true);
                squareSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
                squareSprite.name = "Juice Square";
            }
            return squareSprite;
        }
    }

    /// <summary>
    /// 飄字用的字型。預設是 Unity 內建字型；Web 版要顯示中文時，請指定自己的字型：
    /// JuiceAssets.Font = myFont;
    /// </summary>
    public static Font Font
    {
        get
        {
            if (font == null)
            {
#if UNITY_2022_2_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
            return font;
        }
        set { font = value; }
    }

    static Shader FindUnlitShader()
    {
        // Sprites/Default 在內建管線與 URP 都能用，而且預設會被打包進遊戲。
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        return shader;
    }
}
