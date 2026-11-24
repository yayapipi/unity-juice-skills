using UnityEngine;

/// <summary>
/// 拖尾：快速移動的物件後面拉一條會淡出的線（子彈、衝刺、揮劍）。
///     JuiceTrail.Attach(gameObject, Color.cyan);
///     JuiceTrail.SetEmitting(gameObject, false);   // 暫時不要畫（例如瞬間移動前）
/// 拖尾放在自動建立的子物件「Juice Trail」上，不會動到原本的 Renderer。
/// </summary>
public static class JuiceTrail
{
    const string ChildName = "Juice Trail";

    public static TrailRenderer Attach(GameObject target, Color color, float width = 0.3f, float time = 0.25f)
    {
        if (target == null || !Application.isPlaying) return null;
        Transform child = target.transform.Find(ChildName);
        if (child == null)
        {
            child = new GameObject(ChildName).transform;
            child.SetParent(target.transform, false);
        }
        TrailRenderer trail = child.GetComponent<TrailRenderer>();
        if (trail == null) trail = child.gameObject.AddComponent<TrailRenderer>();

        trail.sharedMaterial = JuiceAssets.TrailMaterial;
        trail.time = time;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 4;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.sortingOrder = JuiceBurst.SortingOrder - 1;
        return trail;
    }

    public static void SetEmitting(GameObject target, bool emitting)
    {
        if (target == null) return;
        Transform child = target.transform.Find(ChildName);
        TrailRenderer trail = child != null ? child.GetComponent<TrailRenderer>() : null;
        if (trail == null) return;
        if (!emitting) trail.Clear();
        trail.emitting = emitting;
    }
}
