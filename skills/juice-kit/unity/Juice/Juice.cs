using UnityEngine;

/// <summary>
/// 常見事件的「一行配方」。每個配方都是好幾種回饋疊在一起，數值是常用的起手值。
/// 在事件發生的那一行加上去就好，不用在場景裡放任何東西：
///
///     Juice.Hit(enemy.gameObject, hitPoint);   // 命中：頓幀＋震動＋閃白＋彈一下＋火花＋音效
///     Juice.Hurt(gameObject);                  // 玩家受傷：更重的頓幀與震動、閃紅
///     Juice.Jump(transform);                   // 起跳：拉長＋塵土＋音效
///     Juice.Land(transform);                   // 落地：壓扁＋塵土＋音效
///     Juice.Pickup(transform.position);        // 撿到東西：小粒子＋音效
///     Juice.Explode(transform.position);       // 爆炸、死亡：長頓幀＋大震動＋鏡頭推近＋爆炸粒子
///     Juice.Damage(hitPoint, 12);              // 傷害數字
///     Juice.Win(transform.position);           // 過關：彩帶＋慢動作＋音效
///
/// 想調整其中一項，就改用下面各個類別（JuiceShake、JuiceTime、JuiceFlash…），
/// 或是用 JuiceFeedback 元件在 Inspector 裡調。
/// </summary>
public static class Juice
{
    public const string Version = "1.0.0";

    /// <summary>命中。power 1 = 一般攻擊，0.5 = 輕擊，2 = 重擊。</summary>
    public static void Hit(GameObject target, Vector3 point, float power = 1f)
    {
        JuiceTime.HitStop(0.05f * power);
        JuiceShake.Add(0.25f * power);
        if (target != null)
        {
            JuiceFlash.Play(target);
            JuiceSquash.Punch(target.transform, 0.15f * power);
        }
        JuiceBurst.Spark(point);
        JuiceSfx.Blip(JuiceBlip.Hit);
    }

    /// <summary>命中（火花噴在目標的位置）。</summary>
    public static void Hit(GameObject target, float power = 1f)
    {
        if (target == null) return;
        Hit(target, target.transform.position, power);
    }

    /// <summary>玩家受傷：比命中更重，閃紅色。</summary>
    public static void Hurt(GameObject target, float power = 1f)
    {
        JuiceTime.HitStop(0.08f * power);
        JuiceShake.Add(0.45f * power);
        if (target != null)
        {
            JuiceFlash.Play(target, new Color(1f, 0.25f, 0.25f), 0.1f);
            JuiceSquash.Squash(target.transform, 0.2f * power);
        }
        JuiceSfx.Blip(JuiceBlip.Hurt);
    }

    /// <summary>起跳。</summary>
    public static void Jump(Transform character)
    {
        if (character == null) return;
        JuiceSquash.Stretch(character, 0.22f);
        JuiceBurst.Dust(FeetOf(character));
        JuiceSfx.Blip(JuiceBlip.Jump, 0.4f);
    }

    /// <summary>落地。impact 1 = 一般落地；從高處落下可以給 1.5–2，會多一點震動。</summary>
    public static void Land(Transform character, float impact = 1f)
    {
        if (character == null) return;
        impact = Mathf.Clamp(impact, 0.3f, 2.5f);
        JuiceSquash.Squash(character, 0.25f * impact);
        JuiceBurst.Dust(FeetOf(character));
        JuiceSfx.Blip(JuiceBlip.Land, 0.4f);
        if (impact >= 1.5f) JuiceShake.Add(0.12f * impact);
    }

    /// <summary>撿到東西。text 有給就會飄字，例如 "+1"。</summary>
    public static void Pickup(Vector3 position, Color? color = null, string text = null)
    {
        Color c = color ?? new Color(1f, 0.85f, 0.3f);
        JuiceBurst.Spawn(position, c, 8, 4f, 0.14f, 0.35f);
        JuiceSfx.Blip(JuiceBlip.Pickup);
        if (!string.IsNullOrEmpty(text)) JuiceFloatingText.Spawn(position, text, c);
    }

    /// <summary>金幣。combo 傳入連續撿到第幾個，音高會一路往上。</summary>
    public static void Coin(Vector3 position, int combo = 0)
    {
        JuiceBurst.Spawn(position, new Color(1f, 0.85f, 0.3f), 6, 3.5f, 0.12f, 0.3f);
        JuiceSfx.PlayCombo(JuiceSfx.GetBlip(JuiceBlip.Coin), combo, 0.45f);
    }

    /// <summary>爆炸或死亡：整個畫面都要有感覺。</summary>
    public static void Explode(Vector3 position, float power = 1f)
    {
        JuiceTime.HitStop(0.1f * power);
        JuiceShake.Add(0.6f * power);
        JuiceShake.Zoom(0.04f * power, 0.3f);
        JuiceBurst.Explosion(position, null, power);
        JuiceSfx.Blip(JuiceBlip.Explosion, 0.6f);
    }

    /// <summary>傷害數字。critical = 暴擊，會變黃、放大、加驚嘆號。</summary>
    public static void Damage(Vector3 position, int amount, bool critical = false)
    {
        if (critical) JuiceFloatingText.Spawn(position, amount + "!", new Color(1f, 0.85f, 0.2f), 1.5f);
        else JuiceFloatingText.Spawn(position, amount.ToString());
    }

    /// <summary>過關、勝利。</summary>
    public static void Win(Vector3 position)
    {
        JuiceBurst.Confetti(position);
        JuiceSfx.Blip(JuiceBlip.PowerUp, 0.6f, 0f);
        JuiceTime.SlowMo(0.4f, 0.4f);
        JuiceShake.Zoom(0.06f, 0.5f);
    }

    /// <summary>失敗、遊戲結束。</summary>
    public static void Lose()
    {
        JuiceSfx.Blip(JuiceBlip.Lose, 0.6f, 0f);
        JuiceTime.SlowMo(0.3f, 0.6f);
        JuiceShake.Add(0.5f);
    }

    /// <summary>按鈕或選項被按下（不是 UI 按鈕元件時用；UI 按鈕直接掛 JuiceButton）。</summary>
    public static void Click(Transform button = null)
    {
        JuiceSfx.Blip(JuiceBlip.Click, 0.35f, 0.05f);
        if (button != null)
        {
            JuiceSquash squash = JuiceSquash.Get(button);
            squash.unscaledTime = true;
            squash.Punch(-0.08f);
        }
    }

    // 角色腳底的位置：有圖就用圖的下緣，沒有就用物件位置
    static Vector3 FeetOf(Transform character)
    {
        Renderer r = character.GetComponentInChildren<Renderer>();
        if (r == null) return character.position;
        Bounds b = r.bounds;
        return new Vector3(b.center.x, b.min.y, b.center.z);
    }
}
