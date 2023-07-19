using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityModManagerNet;

public partial class UnityModManager
{
    public static void OpenUnityFileLog()
    {
        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;
            var folders = new string[] { Application.persistentDataPath, Application.dataPath };
            var files = new string[] { "Player.log", "output_log.txt" };
            foreach (var folder in folders)
            {
                foreach (var file in files)
                {
                    var filepath = Path.Combine(folder, file);
                    if (File.Exists(filepath))
                    {
                        Thread.Sleep(500);
                        Application.OpenURL(filepath);
                        return;
                    }
                }
            }
        }).Start();
    }

    public static Version ParseVersion(string str)
    {
        var array = str.Split('.');
        if (array.Length >= 3)
        {
            var regex = new Regex(@"\D");
            return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")),
                int.Parse(regex.Replace(array[2], "")));
        }

        if (array.Length >= 2)
        {
            var regex = new Regex(@"\D");
            return new Version(int.Parse(regex.Replace(array[0], "")), int.Parse(regex.Replace(array[1], "")));
        }

        if (array.Length >= 1)
        {
            var regex = new Regex(@"\D");
            return new Version(int.Parse(regex.Replace(array[0], "")), 0);
        }

        Logger.Error($"版本字符串“{str}”解析失败！");
        return new Version();
    }

    public static bool IsUnixPlatform()
    {
        var p = (int)Environment.OSVersion.Platform;
        return p == 4 || p == 6 || p == 128;
    }

    public static bool IsMacPlatform()
    {
        var p = (int)Environment.OSVersion.Platform;
        return (p == 6);
    }

    public static bool IsLinuxPlatform()
    {
        var p = (int)Environment.OSVersion.Platform;
        return (p == 4) || (p == 128);
    }
}
/// <summary>
/// [0.22.8.35] 给没用继承MonoBehaviour的类方法中执行协程和延迟执行方法提供支持
/// </summary>
public class DelayToInvoke
{
    private class TaskBehaviour : MonoBehaviour
    {
    }

    private static readonly TaskBehaviour taskBehaviour;
    private static readonly GameObject gameObject;

    //静态构造函数
    static DelayToInvoke()
    {
        gameObject = new GameObject(typeof(DelayToInvoke).FullName, typeof(DelayToInvoke));
        Object.DontDestroyOnLoad(gameObject);
        taskBehaviour = gameObject.AddComponent<TaskBehaviour>();
    }

    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        return taskBehaviour.StartCoroutine(routine);
    }

    public static void StopCoroutine(IEnumerator routine)
    {
        if (null != gameObject) taskBehaviour.StopCoroutine(routine);
    }

    public static void StopCoroutine(ref Coroutine routine)
    {
        if (null != gameObject) taskBehaviour.StopCoroutine(routine);
    }

    public static void StopAllCoroutines()
    {
        if (null != gameObject) taskBehaviour.StopAllCoroutines();
    }

    public static Coroutine DelayToInvokeBySecond(Action action, float delaySeconds)
    {
        return taskBehaviour.StartCoroutine(StartDelayToInvokeBySecond(action, delaySeconds));
    }

    public static Coroutine DelayToInvokeByFrame(Action action, int delayFrames)
    {
        return taskBehaviour.StartCoroutine(StartDelayToInvokeByFrame(action, delayFrames));
    }

    public static Coroutine ActionLoopByTime(float duration, float interval, Action action)
    {
        if (action == null || duration <= 0 || interval <= 0 || duration < interval) return null;
        return taskBehaviour.StartCoroutine(StartActionLoopByTime(duration, interval, action));
    }

    public static Coroutine ActionLoopByCount(int loopCount, float interval, Action action)
    {
        if (action == null || loopCount <= 0 || interval <= 0) return null;
        return taskBehaviour.StartCoroutine(StartActionLoopByCount(loopCount, interval, action));
    }

    private static IEnumerator StartDelayToInvokeBySecond(Action action, float delaySeconds)
    {
        if (delaySeconds > 0) yield return new WaitForSeconds(delaySeconds);
        else yield return null;
        action?.Invoke();
    }

    private static IEnumerator StartDelayToInvokeByFrame(Action action, int delayFrames)
    {
        if (delayFrames > 1) for (var i = 0; i < delayFrames; i++) yield return null;
        else yield return null;
        action?.Invoke();
    }

    private static IEnumerator StartActionLoopByTime(float duration, float interval, Action action)
    {
        yield return new CustomActionLoopByTime(duration, interval, action);
    }

    private static IEnumerator StartActionLoopByCount(int loopCount, float interval, Action action)
    {
        yield return new CustomActionLoopByCount(loopCount, interval, action);
    }

    private class CustomActionLoopByTime : CustomYieldInstruction
    {
        private Action callback;
        private float startTime;
        private float lastTime;
        private float interval;
        private float duration;

        public CustomActionLoopByTime(float _duration, float _interval, Action _callback)
        {
            //记录开始时间
            startTime = Time.time;
            //记录上一次间隔时间
            lastTime = Time.time;
            //记录间隔调用时间
            interval = _interval;
            //记录总时间
            duration = _duration;
            //间隔回调
            callback = _callback;
        }

        //保持协程暂停返回true。让coroutine继续执行返回 false。
        //在MonoBehaviour.Update之后、MonoBehaviour.LateUpdate之前，每帧都会查询keepWaiting属性。
        public override bool keepWaiting
        {
            get
            {
                //此方法返回false表示协程结束
                if (Time.time - startTime >= duration) return false;
                if (Time.time - lastTime >= interval)
                {
                    //更新上一次间隔时间
                    lastTime = Time.time;
                    callback?.Invoke();
                }
                return true;
            }
        }
    }

    private class CustomActionLoopByCount : CustomYieldInstruction
    {
        private Action callback;
        private float lastTime;
        private float interval;
        private int curCount;
        private int loopCount;

        public CustomActionLoopByCount(int _loopCount, float _interval, Action _callback)
        {
            lastTime = Time.time;
            interval = _interval;
            curCount = 0;
            loopCount = _loopCount;
            callback = _callback;
        }

        public override bool keepWaiting
        {
            get
            {
                if (curCount > loopCount)
                {
                    return false;
                }
                else if (Time.time - lastTime >= interval)
                {
                    //更新上一次间隔时间
                    lastTime = Time.time;
                    curCount++;
                    callback?.Invoke();
                }
                return true;
            }
        }
    }
}
/// <summary>
///     [0.18.0]
/// </summary>
public interface ICopyable
{
}

/// <summary>
///     [0.18.0]
/// </summary>
[Flags]
public enum CopyFieldMask
{
    Any = 0,
    Matching = 1,
    Public = 2,
    Serialized = 4,
    SkipNotSerialized = 8,
    OnlyCopyAttr = 16
}

/// <summary>
///     [0.18.0]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CopyFieldsAttribute : Attribute
{
    public CopyFieldMask Mask;

    public CopyFieldsAttribute(CopyFieldMask Mask)
    {
        this.Mask = Mask;
    }
}

/// <summary>
///     [0.18.0]
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class CopyAttribute : Attribute
{
    public string Alias;

    public CopyAttribute()
    {
    }

    public CopyAttribute(string Alias)
    {
        this.Alias = Alias;
    }
}

public static partial class Extensions
{
    /// <summary>
    ///     [0.18.0]
    /// </summary>
    public static void CopyFieldsTo<T1, T2>(this T1 from, ref T2 to)
        where T1 : ICopyable, new()
        where T2 : new()
    {
        object obj = to;
        Utils.CopyFields<T1, T2>(from, obj, CopyFieldMask.OnlyCopyAttr);
        to = (T2)obj;
    }
}

public static partial class Utils
{
    /// <summary>
    ///     [0.18.0]
    /// </summary>
    public static void CopyFields<T1, T2>(object from, object to, CopyFieldMask defaultMask)
        where T1 : new()
        where T2 : new()
    {
        var mask = defaultMask;
        foreach (CopyFieldsAttribute attr in typeof(T1).GetCustomAttributes(typeof(CopyFieldsAttribute), false))
            mask = attr.Mask;

        var fields = typeof(T1).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            var a = new CopyAttribute();
            var attributes = f.GetCustomAttributes(typeof(CopyAttribute), false);
            if (attributes.Length > 0)
            {
                foreach (CopyAttribute a_ in attributes) a = a_;
            }
            else
            {
                if ((mask & CopyFieldMask.OnlyCopyAttr) == 0 && ((mask & CopyFieldMask.SkipNotSerialized) == 0 ||
                                                                 !f.IsNotSerialized)
                                                             && ((mask & CopyFieldMask.Public) > 0 && f.IsPublic
                                                                 || (mask & CopyFieldMask.Serialized) > 0 &&
                                                                 f.GetCustomAttributes(typeof(SerializeField),
                                                                     false).Length > 0
                                                                 || (mask & CopyFieldMask.Public) == 0 &&
                                                                 (mask & CopyFieldMask.Serialized) == 0))
                {
                }
                else
                {
                    continue;
                }
            }

            if (string.IsNullOrEmpty(a.Alias))
                a.Alias = f.Name;

            var f2 = typeof(T2).GetField(a.Alias,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f2 == null)
            {
                if ((mask & CopyFieldMask.Matching) == 0)
                    UnityModManager.Logger.Error($"Field '{typeof(T2).Name}.{a.Alias}' not found");
                continue;
            }

            if (f.FieldType != f2.FieldType)
            {
                UnityModManager.Logger.Error(
                    $"Fields '{typeof(T1).Name}.{f.Name}' and '{typeof(T2).Name}.{f2.Name}' have different types");
                continue;
            }

            f2.SetValue(to, f.GetValue(from));
        }
    }
}
