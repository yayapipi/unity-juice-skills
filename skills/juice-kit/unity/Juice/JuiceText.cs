using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 把文字寫進「任何有 text 屬性的元件」：TextMeshPro、UI Text、TextMesh 都可以。
/// 這樣 Juice Kit 就不用直接相依 TextMeshPro 套件。
///     JuiceText label = JuiceText.Find(gameObject);
///     if (label != null) label.Text = "Hello";
/// </summary>
public sealed class JuiceText
{
    readonly Component component;
    readonly PropertyInfo textProperty;
    readonly PropertyInfo visibleProperty;

    JuiceText(Component component, PropertyInfo textProperty, PropertyInfo visibleProperty)
    {
        this.component = component;
        this.textProperty = textProperty;
        this.visibleProperty = visibleProperty;
    }

    /// <summary>在物件上找第一個有 text 屬性的元件。找不到回傳 null。</summary>
    public static JuiceText Find(GameObject go)
    {
        if (go == null) return null;
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component c = components[i];
            if (c == null || c is Transform) continue;
            PropertyInfo text = GetProperty(c.GetType(), "text", typeof(string));
            if (text == null) continue;
            // TextMeshPro 才有：只顯示前 N 個字，打字機效果用它最乾淨
            PropertyInfo visible = GetProperty(c.GetType(), "maxVisibleCharacters", typeof(int));
            return new JuiceText(c, text, visible);
        }
        return null;
    }

    static PropertyInfo GetProperty(Type type, string name, Type propertyType)
    {
        try
        {
            PropertyInfo p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            if (p != null && p.PropertyType == propertyType && p.CanRead && p.CanWrite) return p;
        }
        catch (AmbiguousMatchException)
        {
        }
        return null;
    }

    public bool IsValid
    {
        get { return component != null; }
    }

    public string Text
    {
        get { return component != null ? (string)textProperty.GetValue(component, null) : ""; }
        set { if (component != null) textProperty.SetValue(component, value, null); }
    }

    /// <summary>是否支援「只顯示前 N 個字」（TextMeshPro）。</summary>
    public bool SupportsVisibleCount
    {
        get { return visibleProperty != null; }
    }

    public void SetVisibleCount(int count)
    {
        if (component != null && visibleProperty != null) visibleProperty.SetValue(component, count, null);
    }
}
