using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityModManagerNet;

/// <summary>
///     [0.18.0]
/// </summary>
public enum DrawType
{
    Auto,
    Ignore,
    Field,
    Slider,
    Toggle,
    ToggleGroup, 
    PopupList,
    KeyBinding,
    /// <summary>
    /// Disabled modifiers. [0.27.5]
    /// </summary>
    KeyBindingNoMod
}

/// <summary>
///     [0.18.0]
/// </summary>
[Flags]
public enum DrawFieldMask
{
    Any = 0,
    Public = 1,
    Serialized = 2,
    SkipNotSerialized = 4,
    OnlyDrawAttr = 8
}

/// <summary>
///     Provides the Draw method for rendering fields. [0.18.0]
/// </summary>
public interface IDrawable
{
    /// <summary>
    ///     Called when values change. For sliders it is called too often.
    /// </summary>
    void OnChange();
}

/// <summary>
///     Specifies which fields to render. [0.18.0]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
public class DrawFieldsAttribute : Attribute
{
    public DrawFieldMask Mask;

    public DrawFieldsAttribute(DrawFieldMask Mask)
    {
        this.Mask = Mask;
    }
}

/// <summary>
///     Sets options for rendering. [0.19.0]
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class DrawAttribute : Attribute
{
    public DrawType Type = DrawType.Auto;
    public string Label;
    public int Width = 0;
    public int Height = 0;
    public double Min = double.MinValue;
    public double Max = double.MaxValue;
    /// <summary>
    /// Rounds a double-precision floating-point value to a specified number of fractional digits, and rounds midpoint values to the nearest even number. 
    /// Default 2
    /// </summary>
    public int Precision = 2;
    /// <summary>
    /// Maximum text length.
    /// </summary>
    public int MaxLength = int.MaxValue;
    /// <summary>
    /// Becomes visible if a field value matches. Use format "FieldName|Value". Supports only string, primitive and enum types.
    /// </summary>
    public string VisibleOn;
    /// <summary>
    /// Becomes invisible if a field value matches. Use format "FieldName|Value". Supports only string, primitive and enum types.
    /// </summary>
    public string InvisibleOn;
    /// <summary>
    /// Applies box style.
    /// </summary>
    public bool Box;
    public bool Collapsible;
    public bool Vertical;
    /// <summary>
    /// (RichText) [0.25.0]
    /// </summary>
    public string Tooltip;
    /// <summary>
    /// Only for string field [0.27.2]
    /// </summary>
    public bool TextArea;

    public DrawAttribute()
    {
    }

    public DrawAttribute(string Label)
    {
        this.Label = Label;
    }

    public DrawAttribute(string Label, DrawType Type)
    {
        this.Label = Label;
        this.Type = Type;
    }

    public DrawAttribute(DrawType Type)
    {
        this.Type = Type;
    }
}

/// <summary>
///     [0.22.14]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field)]
public class HorizontalAttribute : Attribute
{
}

public partial class UnityModManager
{
    public partial class UI
    {
        private static readonly Type[] fieldTypes =
        {
            typeof(int), typeof(long), typeof(float), typeof(double), typeof(int[]), typeof(long[]),
            typeof(float[]), typeof(double[]),
            typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color), typeof(string), typeof(string[])
    };

        private static readonly Type[] sliderTypes = { typeof(int), typeof(long), typeof(float), typeof(double) };
        private static readonly Type[] toggleTypes = { typeof(bool) };
        private static readonly Type[] specialTypes = { typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Color), typeof(KeyBinding), typeof(string) };
        private static readonly float drawHeight = 22;
        private static readonly List<int> collapsibleStates = new List<int>();

        [Obsolete("使用带标题的新版本。")]
        public static bool DrawKeybinding(ref KeyBinding key, GUIStyle style = null, params GUILayoutOption[] option)
        {
            return DrawKeybinding(ref key, null, style, option);
        }

        /// <summary>
        ///     [0.20.8]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        [Obsolete("Use DrawKeybindingSmart.")]
        public static bool DrawKeybinding(ref KeyBinding key, string title, GUIStyle style = null, params GUILayoutOption[] option)
        {
            return DrawKeybinding(ref key, title, 0, style, option);
        }

        /// <summary>
        /// [0.22.15]
        /// </summary>
        /// <returns>
        /// Returns true if the value has changed.
        /// </returns>
        [Obsolete("Use DrawKeybindingSmart.")]
        public static bool DrawKeybinding(ref KeyBinding key, string title, int unique, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var changed = false;
            key ??= new KeyBinding();
            GUILayout.BeginHorizontal();
            var modifiersValue = new byte[] { 1, 2, 4 };
            var modifiersStr = new string[] { "Ctrl", "Shift", "Alt" };
            var modifiers = key.modifiers;
            for (int i = 0; i < modifiersValue.Length; i++)
            {
                if (GUILayout.Toggle((modifiers & modifiersValue[i]) != 0, modifiersStr[i], GUILayout.ExpandWidth(false)))
                {
                    modifiers |= modifiersValue[i];
                }
                else if ((modifiers & modifiersValue[i]) != 0)
                {
                    modifiers ^= modifiersValue[i];
                }
            }
            GUILayout.Label(" + ", GUILayout.ExpandWidth(false));
            var val = key.Index;
            if (PopupToggleGroup(ref val, KeyBinding.KeysName, title, unique, style, option))
            {
                key.Change((KeyCode)Enum.Parse(typeof(KeyCode), KeyBinding.KeysCode[val]), modifiers);
                changed = true;
            }

            if (key.modifiers != modifiers)
            {
                key.modifiers = modifiers;
                changed = true;
            }
            GUILayout.EndHorizontal();

            return changed;
        }

        /// <summary>
        /// (deferred) [0.27.5]
        /// </summary>
        public static void DrawKeybindingSmart(KeyBinding key, string title, GUIStyle style = null, params GUILayoutOption[] option)
        {
            DrawKeybindingSmart(key, title, null, false, style, option);
        }

        /// <summary>
        /// (deferred) [0.27.5]
        /// </summary>
        public static void DrawKeybindingSmart(KeyBinding key, string title, Action<KeyBinding> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            DrawKeybindingSmart(key, title, onChange, false, style, option);
        }

        /// <summary>
        /// (deferred) [0.27.5]
        /// </summary>
        public static void DrawKeybindingSmart(KeyBinding key, string title, Action<KeyBinding> onChange, bool disableModifiers, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (key == null)
            {
                if (onChange == null)
                {
                    throw new ArgumentNullException("key");
                }
                else
                {
                    key = new KeyBinding();
                }
            }

            if (GUILayout.Button(key.ToString(), style ?? GUI.skin.button, option))
            {
                var newKey = new KeyBinding();
                newKey.Change(key.keyCode, key.modifiers);
                var changing = false;

                ShowWindow((window) =>
                {
                    if (changing && Event.current.isKey)
                    {
                        if (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl)
                        {
                            if (newKey.keyCode == KeyCode.None && Event.current.type == EventType.KeyUp)
                            {
                                newKey.modifiers ^= 1;
                                newKey.keyCode = Event.current.keyCode;
                                changing = false;
                            }
                            if (Event.current.type == EventType.KeyDown)
                            {
                                newKey.modifiers |= 1;
                            }
                        }
                        else if (Event.current.keyCode == KeyCode.LeftShift || Event.current.keyCode == KeyCode.RightShift)
                        {
                            if (newKey.keyCode == KeyCode.None && Event.current.type == EventType.KeyUp)
                            {
                                newKey.modifiers ^= 2;
                                newKey.keyCode = Event.current.keyCode;
                                changing = false;
                            }
                            if (Event.current.type == EventType.KeyDown)
                            {
                                newKey.modifiers |= 2;
                            }
                        }
                        else if (Event.current.keyCode == KeyCode.LeftAlt || Event.current.keyCode == KeyCode.RightAlt)
                        {
                            if (newKey.keyCode == KeyCode.None && Event.current.type == EventType.KeyUp)
                            {
                                newKey.modifiers ^= 4;
                                newKey.keyCode = Event.current.keyCode;
                                changing = false;
                            }
                            if (Event.current.type == EventType.KeyDown)
                            {
                                newKey.modifiers |= 4;
                            }
                        }
                        else if (Event.current.keyCode != KeyCode.None)
                        {
                            if (Event.current.type == EventType.KeyUp)
                            {
                                newKey.keyCode = Event.current.keyCode;
                                changing = false;
                            }
                        }
                        if (!changing && disableModifiers)
                        {
                            newKey.modifiers = 0;
                        }
                    }
                    GUILayout.Label($"{(changing ? "按任意键……" : newKey)}", GUI.skin.box, GUILayout.ExpandWidth(true));
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("分配", button))
                    {
                        newKey.Change(KeyCode.None, 0);
                        changing = true;
                    }
                    if (GUILayout.Button("保存", button))
                    {
                        if (key.keyCode != newKey.keyCode || key.modifiers != newKey.modifiers)
                        {
                            key.Change(newKey.keyCode, newKey.modifiers);
                            onChange?.Invoke(key);
                        }
                        window.Close();
                    }
                    if (GUILayout.Button("关闭", button))
                    {
                        window.Close();
                    }

                    GUILayout.EndHorizontal();
                }, title, 6334331);
            }
        }

        /// <summary>
        /// [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns true if the value has changed.
        /// </returns>
        public static bool DrawVector(ref Vector2 vec, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var values = new float[2] { vec.x, vec.y };
            var labels = new string[2] { "x", "y" };
            if (!DrawFloatMultiField(ref values, labels, style, option)) return false;
            vec = new Vector2(values[0], values[1]);
            return true;
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawVector(Vector2 vec, Action<Vector2> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawVector(ref vec, style, option)) onChange(vec);
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns true if the value has changed.
        /// </returns>
        public static bool DrawVector(ref Vector3 vec, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var values = new float[3] { vec.x, vec.y, vec.z };
            var labels = new string[3] { "x", "y", "z" };
            if (!DrawFloatMultiField(ref values, labels, style, option)) return false;
            vec = new Vector3(values[0], values[1], values[2]);
            return true;
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawVector(Vector3 vec, Action<Vector3> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawVector(ref vec, style, option)) onChange(vec);
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        public static bool DrawVector(ref Vector4 vec, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var values = new float[4] { vec.x, vec.y, vec.z, vec.w };
            var labels = new string[4] { "x", "y", "z", "w" };
            if (DrawFloatMultiField(ref values, labels, style, option))
            {
                vec = new Vector4(values[0], values[1], values[2], values[3]);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawVector(Vector4 vec, Action<Vector4> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawVector(ref vec, style, option)) onChange(vec);
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        public static bool DrawColor(ref Color vec, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var values = new float[4] { vec.r, vec.g, vec.b, vec.a };
            var labels = new string[4] { "r", "g", "b", "a" };
            if (!DrawFloatMultiField(ref values, labels, style, option)) return false;
            vec = new Color(values[0], values[1], values[2], values[3]);
            return true;
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawColor(Color vec, Action<Color> onChange, GUIStyle style = null,
            params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawColor(ref vec, style, option)) onChange(vec);
        }

        /// <summary>
        ///     [0.18.0]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        public static bool DrawFloatMultiField(ref float[] values, string[] labels, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentNullException(nameof(values));
            if (labels == null || labels.Length == 0)
                throw new ArgumentNullException(nameof(labels));
            if (values.Length != labels.Length)
                throw new ArgumentOutOfRangeException(nameof(labels));

            var changed = false;
            var result = new float[values.Length];

            for (var i = 0; i < values.Length; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(labels[i], GUILayout.ExpandWidth(false));
                var str = GUILayout.TextField(values[i].ToString("f6"), style ?? GUI.skin.textField, option);
                GUILayout.EndHorizontal();
                if (string.IsNullOrEmpty(str))
                {
                    result[i] = 0;
                }
                else
                {
                    if (float.TryParse(str, NumberStyles.Any, NumberFormatInfo.CurrentInfo, out var num))
                        result[i] = num;
                    else
                        result[i] = 0;
                }

                if (!result[i].Equals(values[i])) changed = true;
            }

            values = result;
            return changed;
        }

        /// <summary>
        ///     [0.19.0]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        public static bool DrawFloatField(ref float value, string label, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var old = value;
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            var str = GUILayout.TextField(value.ToString("f6"), style ?? GUI.skin.textField, option);
            if (string.IsNullOrEmpty(str))
            {
                value = 0;
            }
            else
            {
                if (float.TryParse(str, NumberStyles.Float, NumberFormatInfo.CurrentInfo, out var num))
                    value = num;
                else
                    value = 0;
            }

            return !value.Equals(old);
        }

        /// <summary>
        ///     [0.19.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawFloatField(float value, string label, Action<float> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawFloatField(ref value, label, style, option)) onChange(value);
        }

        /// <summary>
        ///     [0.19.0]
        /// </summary>
        /// <returns>
        ///     Returns true if the value has changed.
        /// </returns>
        public static bool DrawIntField(ref int value, string label, GUIStyle style = null, params GUILayoutOption[] option)
        {
            var old = value;
            GUILayout.Label(label, GUILayout.ExpandWidth(false));
            var str = GUILayout.TextField(value.ToString(), style ?? GUI.skin.textField, option);
            if (string.IsNullOrEmpty(str))
                value = 0;
            else
                value = int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out var num)
                    ? num
                    : 0;

            return value != old;
        }

        /// <summary>
        ///     [0.19.0]
        /// </summary>
        /// <returns>
        /// Returns result via onChange.
        /// </returns>
        public static void DrawIntField(int value, string label, Action<int> onChange, GUIStyle style = null, params GUILayoutOption[] option)
        {
            if (onChange == null) throw new ArgumentNullException(nameof(onChange));
            if (DrawIntField(ref value, label, style, option)) onChange(value);
        }

        /// <summary>
        /// # is a feature of [0.24.2]
        /// </summary>
        private static bool DependsOn(string str, object container, Type type, ModEntry mod)
        {
            var param = str.Split('|');
            if (param.Length != 2)
                throw new Exception(
                    $"VisibleOn/InvisibleOn({str})必须提供两个参数，字段名称和值, 例如：FieldName|True或#PropertyName|True。");
            var isField = !str.StartsWith("#");
            if (isField)
            {
                var dependsOnField = type.GetField(param[0], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dependsOnField == null)
                {
                    throw new Exception($"找不到字段“{param[0]}”！请在属性的开头插入#！");
                }
                if (!dependsOnField.FieldType.IsPrimitive && !dependsOnField.FieldType.IsEnum)
                {
                    throw new Exception($"不支持的类型：“{dependsOnField.FieldType.Name}”！");
                }
                object dependsOnValue = null;
                if (dependsOnField.FieldType.IsEnum)
                {
                    try
                    {
                        dependsOnValue = Enum.Parse(dependsOnField.FieldType, param[1]);
                        if (dependsOnValue == null)
                        {
                            throw new Exception($"不能解析值“{param[1]}”！");
                        }
                    }
                    catch (Exception e)
                    {
                        mod.Logger.Log($"解析值VisibleOn/InvisibleOn({str})时出错！");
                        throw e;
                    }
                }
                else if (dependsOnField.FieldType == typeof(string))
                {
                    dependsOnValue = param[1];
                }
                else
                {
                    try
                    {
                        dependsOnValue = Convert.ChangeType(param[1], dependsOnField.FieldType);
                        if (dependsOnValue == null)
                        {
                            throw new Exception($"不能解析值“{param[1]}”！");
                        }
                    }
                    catch (Exception e)
                    {
                        mod.Logger.Log($"解析值VisibleOn/InvisibleOn({str})时出错！");
                        throw e;
                    }
                }
                var value = dependsOnField.GetValue(container);
                return value.GetHashCode() == dependsOnValue.GetHashCode();
            }
            else
            {
                param[0] = param[0].TrimStart('#');
                var dependsOnProperty = type.GetProperty(param[0], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dependsOnProperty == null)
                {
                    throw new Exception($"找不到属性“{param[0]}”！");
                }
                if (!dependsOnProperty.PropertyType.IsPrimitive && !dependsOnProperty.PropertyType.IsEnum)
                {
                    throw new Exception($"不支持的属性：“{dependsOnProperty.PropertyType.Name}”！");
                }
                object dependsOnValue = null;
                if (dependsOnProperty.PropertyType.IsEnum)
                {
                    try
                    {
                        dependsOnValue = Enum.Parse(dependsOnProperty.PropertyType, param[1]);
                        if (dependsOnValue == null)
                        {
                            throw new Exception($"不能解析值“{param[1]}”！");
                        }
                    }
                    catch (Exception e)
                    {
                        mod.Logger.Log($"解析值VisibleOn/InvisibleOn({str})时出错！");
                        throw e;
                    }
                }
                else if (dependsOnProperty.PropertyType == typeof(string))
                {
                    dependsOnValue = param[1];
                }
                else
                {
                    try
                    {
                        dependsOnValue = Convert.ChangeType(param[1], dependsOnProperty.PropertyType);
                        if (dependsOnValue == null)
                        {
                            throw new Exception($"不能解析值“{param[1]}”！");
                        }
                    }
                    catch (Exception e)
                    {
                        mod.Logger.Log($"解析值VisibleOn/InvisibleOn({str})时出错！");
                        throw e;
                    }
                }
                var value = dependsOnProperty.GetValue(container, null);
                return value.GetHashCode() == dependsOnValue.GetHashCode();
            }
        }

        private static void BeginTooltip(string tooltip) { }

        private static void EndTooltip(string tooltip, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (!string.IsNullOrEmpty(tooltip))
            {
                GUILayout.Box(Textures.Question, style ?? IconStyle, options);
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(tooltip);
                }
            }
        }

        private static void BeginHorizontalTooltip(DrawAttribute a)
        {
            if (!string.IsNullOrEmpty(a.Tooltip))
            {
                if (a.Vertical)
                    GUILayout.BeginHorizontal();
            }
        }

        private static void EndHorizontalTooltip(DrawAttribute a)
        {
            if (!string.IsNullOrEmpty(a.Tooltip))
            {
                GUILayout.Box(Textures.Question, IconStyle);
                if (Event.current.type == EventType.Repaint && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    ShowTooltip(a.Tooltip);
                }
                if (a.Vertical)
                    GUILayout.EndHorizontal();
            }
        }

        private static void ShowTooltip(string str)
        {
            Instance.mTooltip = new GUIContent(str);
        }

        private static bool Draw(object container, Type type, ModEntry mod, DrawFieldMask defaultMask, int unique)
        {
            var changed = false;
            var options = new List<GUILayoutOption>();
            var mask = defaultMask;
            foreach (DrawFieldsAttribute attr in type.GetCustomAttributes(typeof(DrawFieldsAttribute), false))
                mask = attr.Mask;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var f in fields)
            {
                var a = new DrawAttribute();
                var attributes = f.GetCustomAttributes(typeof(DrawAttribute), false);
                if (attributes.Length > 0)
                {
                    foreach (DrawAttribute a_ in attributes)
                    {
                        a = a_;
                        a.Width = a.Width != 0 ? Scale(a.Width) : 0;
                        a.Height = a.Height != 0 ? Scale(a.Height) : 0;
                    }

                    if (a.Type == DrawType.Ignore)
                        continue;

                    if (!string.IsNullOrEmpty(a.VisibleOn))
                    {
                        if (!DependsOn(a.VisibleOn, container, type, mod)) continue;
                    }
                    else if (!string.IsNullOrEmpty(a.InvisibleOn))
                    {
                        if (DependsOn(a.InvisibleOn, container, type, mod)) continue;
                    }
                }
                else
                {
                    if ((mask & DrawFieldMask.OnlyDrawAttr) == 0 && ((mask & DrawFieldMask.SkipNotSerialized) ==
                                                                     0 || !f.IsNotSerialized)
                                                                 && ((mask & DrawFieldMask.Public) > 0 && f.IsPublic
                                                                     || (mask & DrawFieldMask.Serialized) > 0 &&
                                                                     f.GetCustomAttributes(typeof(SerializeField),
                                                                         false).Length > 0
                                                                     || (mask & DrawFieldMask.Public) == 0 &&
                                                                     (mask & DrawFieldMask.Serialized) == 0))
                        foreach (RangeAttribute a_ in f.GetCustomAttributes(typeof(RangeAttribute), false))
                        {
                            a.Type = DrawType.Slider;
                            a.Min = a_.min;
                            a.Max = a_.max;
                            break;
                        }
                    else
                        continue;
                }

                foreach (SpaceAttribute a_ in f.GetCustomAttributes(typeof(SpaceAttribute), false))
                    GUILayout.Space(Scale((int)a_.height));

                foreach (HeaderAttribute a_ in f.GetCustomAttributes(typeof(HeaderAttribute), false))
                    GUILayout.Label(a_.header, BoldFontStyle, GUILayout.ExpandWidth(false));

                var fieldName = a.Label ?? f.Name;

                if ((f.FieldType.IsClass && !f.FieldType.IsArray ||
                     f.FieldType.IsValueType && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum) &&
                    !Array.Exists(specialTypes, x => x == f.FieldType))
                {
                    defaultMask = mask;
                    foreach (DrawFieldsAttribute attr in f.GetCustomAttributes(typeof(DrawFieldsAttribute), false))
                        defaultMask = attr.Mask;

                    var box = a.Box || a.Collapsible && collapsibleStates.Exists(x => x == f.MetadataToken);
                    var horizontal = f.GetCustomAttributes(typeof(HorizontalAttribute), false).Length > 0 || f.FieldType.GetCustomAttributes(typeof(HorizontalAttribute), false).Length > 0;
                    if (horizontal)
                    {
                        GUILayout.BeginHorizontal(box ? "box" : "");
                        box = false;
                    }

                    if (a.Collapsible)
                        GUILayout.BeginHorizontal();

                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        BeginHorizontalTooltip(a);
                        GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                        EndHorizontalTooltip(a);
                    }

                    var visible = true;
                    if (a.Collapsible)
                    {
                        if (!string.IsNullOrEmpty(fieldName))
                            GUILayout.Space(5);
                        visible = collapsibleStates.Exists(x => x == f.MetadataToken);
                        if (GUILayout.Button(visible ? "Hide" : "Show", GUILayout.ExpandWidth(false)))
                        {
                            if (visible)
                                collapsibleStates.Remove(f.MetadataToken);
                            else
                                collapsibleStates.Add(f.MetadataToken);
                        }

                        GUILayout.EndHorizontal();
                    }

                    if (visible)
                    {
                        if (box)
                            GUILayout.BeginVertical("box");
                        var val = f.GetValue(container);
                        if (typeof(Object).IsAssignableFrom(f.FieldType) && val is Object obj)
                        {
                            GUILayout.Label(obj.name, GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            if (Draw(val, f.FieldType, mod, defaultMask, f.Name.GetHashCode() + unique))
                            {
                                changed = true;
                                f.SetValue(container, val);
                            }
                        }

                        if (box)
                            GUILayout.EndVertical();
                    }

                    if (horizontal)
                        GUILayout.EndHorizontal();
                    continue;
                }

                options.Clear();
                if (a.Type == DrawType.Auto)
                {
                    if (Array.Exists(fieldTypes, x => x == f.FieldType))
                    {
                        a.Type = DrawType.Field;
                    }
                    else if (Array.Exists(toggleTypes, x => x == f.FieldType))
                    {
                        a.Type = DrawType.Toggle;
                    }
                    else if (f.FieldType.IsEnum)
                    {
                        if (f.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
                            a.Type = DrawType.PopupList;
                    }
                    else if (f.FieldType == typeof(KeyBinding))
                    {
                        a.Type = DrawType.KeyBinding;
                    }
                }

                switch (a.Type)
                {
                    case DrawType.Field
                        when !Array.Exists(fieldTypes, x => x == f.FieldType) && !f.FieldType.IsArray:
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.Field}！");
                    case DrawType.Field:
                        {
                            options.Add(a.Width != 0 ? GUILayout.Width(a.Width) : GUILayout.Width(Scale(100)));
                            options.Add(a.Height != 0 ? GUILayout.Height(a.Height) : GUILayout.Height(Scale(a.TextArea ? (int)drawHeight * 3 : (int)drawHeight)));
                            if (f.FieldType == typeof(Vector2))
                            {
                                if (a.Vertical)
                                    GUILayout.BeginVertical();
                                else
                                    GUILayout.BeginHorizontal();
                                BeginHorizontalTooltip(a);
                                GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                EndHorizontalTooltip(a);
                                if (!a.Vertical)
                                    GUILayout.Space(Scale(5));
                                var vec = (Vector2)f.GetValue(container);
                                if (DrawVector(ref vec, null, options.ToArray()))
                                {
                                    f.SetValue(container, vec);
                                    changed = true;
                                }

                                if (a.Vertical)
                                {
                                    GUILayout.EndVertical();
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else if (f.FieldType == typeof(Vector3))
                            {
                                if (a.Vertical)
                                    GUILayout.BeginVertical();
                                else
                                    GUILayout.BeginHorizontal();
                                BeginHorizontalTooltip(a);
                                GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                EndHorizontalTooltip(a);
                                if (!a.Vertical)
                                    GUILayout.Space(Scale(5));
                                var vec = (Vector3)f.GetValue(container);
                                if (DrawVector(ref vec, null, options.ToArray()))
                                {
                                    f.SetValue(container, vec);
                                    changed = true;
                                }
                                if (a.Vertical)
                                {
                                    GUILayout.EndVertical();
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else if (f.FieldType == typeof(Vector4))
                            {
                                if (a.Vertical)
                                    GUILayout.BeginVertical();
                                else
                                    GUILayout.BeginHorizontal();
                                BeginHorizontalTooltip(a);
                                GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                EndHorizontalTooltip(a);
                                if (!a.Vertical)
                                    GUILayout.Space(Scale(5));
                                var vec = (Vector4)f.GetValue(container);
                                if (DrawVector(ref vec, null, options.ToArray()))
                                {
                                    f.SetValue(container, vec);
                                    changed = true;
                                }
                                if (a.Vertical)
                                {
                                    GUILayout.EndVertical();
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else if (f.FieldType == typeof(Color))
                            {
                                if (a.Vertical)
                                    GUILayout.BeginVertical();
                                else
                                    GUILayout.BeginHorizontal();
                                BeginHorizontalTooltip(a);
                                GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                EndHorizontalTooltip(a);
                                if (!a.Vertical)
                                    GUILayout.Space(Scale(5));
                                var vec = (Color)f.GetValue(container);
                                if (DrawColor(ref vec, null, options.ToArray()))
                                {
                                    f.SetValue(container, vec);
                                    changed = true;
                                }
                                if (a.Vertical)
                                {
                                    GUILayout.EndVertical();
                                }
                                else
                                {
                                    GUILayout.FlexibleSpace();
                                    GUILayout.EndHorizontal();
                                }
                            }
                            else
                            {
                                var obj = f.GetValue(container);
                                Type elementType = null;
                                object[] values = null;
                                if (f.FieldType.IsArray)
                                {
                                    if (obj is IEnumerable array)
                                    {
                                        values = array.Cast<object>().ToArray();
                                        elementType = obj.GetType().GetElementType();
                                    }
                                }
                                else
                                {
                                    values = new[] { obj };
                                    elementType = obj.GetType();
                                }

                                if (values == null) continue;

                                var _changed = false;
                                a.Vertical = a.Vertical || f.FieldType.IsArray;
                                if (a.Vertical)
                                    GUILayout.BeginVertical();
                                else
                                    GUILayout.BeginHorizontal();
                                if (f.FieldType.IsArray)
                                {
                                    GUILayout.BeginHorizontal();
                                    BeginHorizontalTooltip(a);
                                    GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                    EndHorizontalTooltip(a);
                                    GUILayout.Space(Scale(5));
                                    if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                                    {
                                        Array.Resize(ref values, Math.Min(values.Length + 1, int.MaxValue));
                                        values[values.Length - 1] = Convert.ChangeType("0", elementType);
                                        _changed = true;
                                        changed = true;
                                    }

                                    if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
                                    {
                                        Array.Resize(ref values, Math.Max(values.Length - 1, 0));
                                        _changed = true;
                                        changed = true;
                                    }

                                    GUILayout.EndHorizontal();
                                }
                                else
                                {
                                    BeginHorizontalTooltip(a);
                                    GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                                    EndHorizontalTooltip(a);
                                }

                                if (!a.Vertical)
                                    GUILayout.Space(Scale(5));

                                if (values.Length > 0)
                                {
                                    var isFloat = f.FieldType == typeof(float) || f.FieldType == typeof(double);
                                    for (var i = 0; i < values.Length; i++)
                                    {
                                        var val = values[i].ToString();
                                        if (a.Precision >= 0 && isFloat)
                                            if (double.TryParse(val, NumberStyles.Float, NumberFormatInfo.CurrentInfo,
                                                    out var num))
                                                val = num.ToString($"f{a.Precision}");
                                        if (f.FieldType.IsArray)
                                        {
                                            GUILayout.BeginHorizontal();
                                            GUILayout.Label($"  [{i}] ", GUILayout.ExpandWidth(false));
                                        }
                                        if (elementType == typeof(string))
                                            options.Add(GUILayout.ExpandWidth(true));
                                        string result;
                                        if (elementType == typeof(string))
                                            result = a.TextArea ? GUILayout.TextArea(val, a.MaxLength, options.ToArray()) : GUILayout.TextField(val, a.MaxLength, options.ToArray());
                                        else
                                            result = GUILayout.TextField(val, options.ToArray());
                                        if (f.FieldType.IsArray) GUILayout.EndHorizontal();
                                        if (result == val) continue;
                                        if (elementType != typeof(string) && string.IsNullOrEmpty(result))
                                        {
                                            result = "0";
                                        }
                                        else
                                        {
                                            if (double.TryParse(result, NumberStyles.Float,
                                                    NumberFormatInfo.CurrentInfo, out var num))
                                            {
                                                num = Math.Max(num, a.Min);
                                                num = Math.Min(num, a.Max);
                                                result = num.ToString();
                                            }
                                            else
                                            {
                                                result = "0";
                                            }
                                        }

                                        values[i] = Convert.ChangeType(result, elementType);
                                        changed = true;
                                        _changed = true;
                                    }
                                }

                                if (_changed)
                                {
                                    if (f.FieldType.IsArray)
                                    {
                                        if (elementType == typeof(float))
                                            f.SetValue(container, Array.ConvertAll(values, x => (float)x));
                                        else if (elementType == typeof(int))
                                            f.SetValue(container, Array.ConvertAll(values, x => (int)x));
                                        else if (elementType == typeof(long))
                                            f.SetValue(container, Array.ConvertAll(values, x => (long)x));
                                        else if (elementType == typeof(double))
                                            f.SetValue(container, Array.ConvertAll(values, x => (double)x));
                                        else if (elementType == typeof(string))
                                            f.SetValue(container, Array.ConvertAll(values, x => (string)x));
                                    }
                                    else
                                    {
                                        f.SetValue(container, values[0]);
                                    }
                                }

                                if (a.Vertical)
                                    GUILayout.EndVertical();
                                else
                                    GUILayout.EndHorizontal();
                            }

                            break;
                        }
                    case DrawType.Slider when !Array.Exists(sliderTypes, x => x == f.FieldType):
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.Slider}！");
                    case DrawType.Slider:
                        {
                            options.Add(a.Width != 0 ? GUILayout.Width(a.Width) : GUILayout.Width(Scale(200)));
                            options.Add(a.Height != 0
                                ? GUILayout.Height(a.Height)
                                : GUILayout.Height(Scale((int)drawHeight)));
                            if (a.Vertical)
                                GUILayout.BeginVertical();
                            else
                                GUILayout.BeginHorizontal();
                            BeginHorizontalTooltip(a);
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            EndHorizontalTooltip(a);
                            if (!a.Vertical)
                                GUILayout.Space(Scale(5));
                            var val = f.GetValue(container).ToString();
                            if (!double.TryParse(val, NumberStyles.Float, NumberFormatInfo.CurrentInfo, out var num))
                                num = 0;
                            if (a.Vertical)
                                GUILayout.BeginHorizontal();
                            var fnum = (float)num;
                            var result =
                                GUILayout.HorizontalSlider(fnum, (float)a.Min, (float)a.Max, options.ToArray());
                            if (!a.Vertical)
                                GUILayout.Space(Scale(5));
                            GUILayout.Label(result.ToString(), GUILayout.ExpandWidth(false),
                                GUILayout.Height(Scale((int)drawHeight)));
                            if (a.Vertical)
                                GUILayout.EndHorizontal();
                            if (a.Vertical)
                                GUILayout.EndVertical();
                            else
                                GUILayout.EndHorizontal();
                            if (!result.Equals(fnum))
                            {
                                if ((f.FieldType == typeof(float) || f.FieldType == typeof(double)) && a.Precision >= 0)
                                    result = (float)Math.Round(result, a.Precision);
                                f.SetValue(container, Convert.ChangeType(result, f.FieldType));
                                changed = true;
                            }

                            break;
                        }
                    case DrawType.Toggle when !Array.Exists(toggleTypes, x => x == f.FieldType):
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.Toggle}！");
                    case DrawType.Toggle:
                        {
                            options.Add(GUILayout.ExpandWidth(false));
                            options.Add(a.Height != 0
                                ? GUILayout.Height(a.Height)
                                : GUILayout.Height(Scale((int)drawHeight)));
                            if (a.Vertical)
                                GUILayout.BeginVertical();
                            else
                                GUILayout.BeginHorizontal();
                            BeginHorizontalTooltip(a);
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            EndHorizontalTooltip(a);
                            var val = (bool)f.GetValue(container);
                            var result = GUILayout.Toggle(val, "", options.ToArray());
                            if (a.Vertical)
                                GUILayout.EndVertical();
                            else
                                GUILayout.EndHorizontal();
                            if (result != val)
                            {
                                f.SetValue(container, Convert.ChangeType(result, f.FieldType));
                                changed = true;
                            }

                            break;
                        }
                    case DrawType.ToggleGroup when !f.FieldType.IsEnum:
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.ToggleGroup}！");
                    case DrawType.ToggleGroup:
                        {
                            options.Add(GUILayout.ExpandWidth(false));
                            options.Add(a.Height != 0
                                ? GUILayout.Height(a.Height)
                                : GUILayout.Height(Scale((int)drawHeight)));
                            if (a.Vertical)
                                GUILayout.BeginVertical();
                            else
                                GUILayout.BeginHorizontal();
                            BeginHorizontalTooltip(a);
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            EndHorizontalTooltip(a);
                            if (!a.Vertical)
                                GUILayout.Space(Scale(5));
                            var names = Enum.GetNames(f.FieldType);
                            var values = Enum.GetValues(f.FieldType);
                            var val = f.GetValue(container);
                            var index = Array.IndexOf(values, val);
                            if (ToggleGroup(ref index, names, null, options.ToArray()))
                            {
                                var v = Enum.Parse(f.FieldType, names[index]);
                                f.SetValue(container, v);
                                changed = true;
                            }

                            if (a.Vertical)
                                GUILayout.EndVertical();
                            else
                                GUILayout.EndHorizontal();
                            break;
                        }
                    case DrawType.PopupList when !f.FieldType.IsEnum:
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.PopupList}！");
                    case DrawType.PopupList:
                        {
                            options.Add(GUILayout.ExpandWidth(false));
                            options.Add(a.Height != 0
                                ? GUILayout.Height(a.Height)
                                : GUILayout.Height(Scale((int)drawHeight)));
                            if (a.Vertical)
                                GUILayout.BeginVertical();
                            else
                                GUILayout.BeginHorizontal();
                            BeginHorizontalTooltip(a);
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            EndHorizontalTooltip(a);
                            if (!a.Vertical)
                                GUILayout.Space(Scale(5));
                            var names = Enum.GetNames(f.FieldType);
                            var values = Enum.GetValues(f.FieldType);
                            var val = f.GetValue(container);
                            var index = Array.IndexOf(values, val);
                            if (PopupToggleGroup(ref index, names, fieldName, unique, null, options.ToArray()))
                            {
                                var v = Enum.Parse(f.FieldType, names[index]);
                                f.SetValue(container, v);
                                changed = true;
                            }

                            if (a.Vertical)
                                GUILayout.EndVertical();
                            else
                                GUILayout.EndHorizontal();
                            break;
                        }
                    case DrawType.KeyBinding | DrawType.KeyBindingNoMod when f.FieldType != typeof(KeyBinding):
                        throw new Exception($"类型 {f.FieldType} 不能被描绘为 {DrawType.KeyBinding}！");
                    case DrawType.KeyBinding | DrawType.KeyBindingNoMod:
                        {
                            if (a.Vertical)
                                GUILayout.BeginVertical();
                            else
                                GUILayout.BeginHorizontal();
                            BeginHorizontalTooltip(a);
                            GUILayout.Label(fieldName, GUILayout.ExpandWidth(false));
                            EndHorizontalTooltip(a);
                            if (!a.Vertical)
                                GUILayout.Space(Scale(5));
                            var key = (KeyBinding)f.GetValue(container);
                            key ??= new KeyBinding();

                            DrawKeybindingSmart(key, fieldName, (k) =>
                            {
                                f.SetValue(container, k);
                                changed = true;
                            }, a.Type == DrawType.KeyBindingNoMod, null, options.ToArray());

                            if (a.Vertical)
                            {
                                GUILayout.EndVertical();
                            }
                            else
                            {
                                GUILayout.FlexibleSpace();
                                GUILayout.EndHorizontal();
                            }

                            break;
                        }
                }
            }

            return changed;
        }

        /// <summary>
        ///     Renders fields [0.18.0]
        /// </summary>
        public static void DrawFields<T>(ref T container, ModEntry mod, DrawFieldMask defaultMask,
            Action onChange = null) where T : new()
        {
            DrawFields<T>(ref container, mod, 0, defaultMask, onChange);
        }

        /// <summary>
        /// Renders fields [0.22.15]
        /// </summary>
        public static void DrawFields<T>(ref T container, ModEntry mod, int unique, DrawFieldMask defaultMask, Action onChange = null) where T : new()
        {
            object obj = container;
            var changed = Draw(obj, typeof(T), mod, defaultMask, unique);
            if (!changed) return;
            container = (T)obj;
            if (onChange == null) return;
            try
            {
                onChange();
            }
            catch (Exception e)
            {
                mod.Logger.LogException(e);
            }
        }
    }
}

public static partial class Extensions
{
    /// <summary>
    ///     Renders fields with mask OnlyDrawAttr. [0.18.0]
    /// </summary>
    public static void Draw<T>(this T instance, UnityModManager.ModEntry mod) where T : class, IDrawable, new()
    {
        UnityModManager.UI.DrawFields(ref instance, mod, DrawFieldMask.OnlyDrawAttr, instance.OnChange);
    }

    /// <summary>
    /// Renders fields with mask OnlyDrawAttr. [0.22.15]
    /// </summary>
    public static void Draw<T>(this T instance, UnityModManager.ModEntry mod, int unique) where T : class, IDrawable, new()
    {
        UnityModManager.UI.DrawFields(ref instance, mod, unique, DrawFieldMask.OnlyDrawAttr, instance.OnChange);
    }
}