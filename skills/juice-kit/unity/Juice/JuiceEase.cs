using UnityEngine;

/// <summary>緩動曲線的種類。預覽：https://easings.net</summary>
public enum JuiceEaseType
{
    Linear,
    InQuad,
    OutQuad,
    InOutQuad,
    OutCubic,
    InOutCubic,
    OutBack,
    OutElastic,
    OutBounce
}

/// <summary>
/// 緩動曲線。把 0–1 的進度換成有加減速的 0–1。
/// 用法：float e = JuiceEase.Evaluate(JuiceEaseType.OutBack, t);
/// </summary>
public static class JuiceEase
{
    public static float Evaluate(JuiceEaseType type, float t)
    {
        t = Mathf.Clamp01(t);
        switch (type)
        {
            case JuiceEaseType.InQuad:
                return t * t;
            case JuiceEaseType.OutQuad:
                return 1f - (1f - t) * (1f - t);
            case JuiceEaseType.InOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
            case JuiceEaseType.OutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);
            case JuiceEaseType.InOutCubic:
                return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
            case JuiceEaseType.OutBack:
            {
                const float c1 = 1.70158f;
                const float c3 = c1 + 1f;
                float u = t - 1f;
                return 1f + c3 * u * u * u + c1 * u * u;
            }
            case JuiceEaseType.OutElastic:
            {
                if (t <= 0f) return 0f;
                if (t >= 1f) return 1f;
                const float c4 = 2f * Mathf.PI / 3f;
                return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
            }
            case JuiceEaseType.OutBounce:
            {
                const float n1 = 7.5625f;
                const float d1 = 2.75f;
                if (t < 1f / d1) return n1 * t * t;
                if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                t -= 2.625f / d1;
                return n1 * t * t + 0.984375f;
            }
            default:
                return t;
        }
    }
}
