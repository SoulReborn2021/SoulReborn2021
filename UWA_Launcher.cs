#define UWA_GOT

using UnityEngine;
using System.Collections;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
#if false
using UWAPlatform = UWA.IOS;            
#elif UNITY_ANDROID
using UWAPlatform = UWA.Android;        
#elif UNITY_STANDALONE_WIN
using UWAPlatform = UWA.Windows;        
#else   
using UWAPlatform = UWA;
namespace UWA
{
    class GUIWrapper : MonoBehaviour
    {
        public static bool ControlByPoco;
    }
    class UWAEngine
    {
        public static int FrameId;
        public static void StaticInit() { }
        public enum Mode { Test };
        public static void Start(Mode mode) { }
        public static void Stop() { }
        public static void PushSample(string sampleName) { }
        public static void PopSample() { }
        public static void LogValue(string valueName, float value) { }
        public static void LogValue(string valueName, Vector3 value) { }
        public static void LogValue(string valueName, int value) { }
        public static void LogValue(string valueName, bool value) { }
        public static void AddMarker(string valueName) { }
        public static void SetOverrideLuaLib(string luaLib) { }
        public static void Upload(Action<bool> callback, string user, string password, string projectName, int timeLimitS){}
        public static void Upload(Action<bool> callback, string user, string password, int projectId, int timeLimitS){}
        public static void Tag(string tag){}
        public static void SetUIActive(bool active){}
        public static void AssetDumpInOverview(){}
    }
}
#endif

[ExecuteInEditMode]
public class UWA_Launcher : MonoBehaviour {

#if false
    [DllImport("__Internal")]
    private static extern void RegisterPluginLoad();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void OnLoad()
    {
        RegisterPluginLoad();
    }
#endif

    
    
    
    [Tooltip("Enable this to make UWA GOT controlled by Poco. [Not supported on IL2CPP]")]
    public bool ControlByPoco = false;
    
    void Awake () { Refresh(true); }

#if UNITY_EDITOR
    void OnEnable() { Refresh(true); }
#endif

    private void Refresh(bool removeOthers)
    {
        UWAPlatform.GUIWrapper wrapper = gameObject.GetComponent<UWAPlatform.GUIWrapper>();
        if (wrapper == null)
        {
            wrapper = gameObject.AddComponent<UWAPlatform.GUIWrapper>();
        }
        UWAPlatform.GUIWrapper.ControlByPoco = ControlByPoco;

#if UNITY_EDITOR
        if (removeOthers)
        {
            Component[] coms = gameObject.GetComponents<Component>();
            for (int i = 0; i < coms.Length; i++)
            {
                if (coms[i] != null &&
                    coms[i] != this &&
                    coms[i] != wrapper &&
                    coms[i].GetType() != typeof(Transform))
                    DestroyImmediate(coms[i]);
            }
        }
#endif
    }
}

public class UWAEngine
{
    
    
    
    public static void StaticInit(bool poco = false)
    {
        UWAPlatform.UWAEngine.StaticInit();
        UWAPlatform.GUIWrapper.ControlByPoco = poco;
    }

    
    
    
    public static int FrameId { get { return UWAPlatform.UWAEngine.FrameId; } }

    
    
    
    public enum Mode
    {
        Overview = 0,
        Mono = 1,
        Assets = 2,
        Lua = 3,
        Unset = 4,
    }

    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void Start(Mode mode)
    {
        UWAPlatform.UWAEngine.Start((UWAPlatform.UWAEngine.Mode)mode);
    }

    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void Stop()
    {
        UWAPlatform.UWAEngine.Stop();
    }


    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void Tag(string tag)
    {
        UWAPlatform.UWAEngine.Tag(tag);
    }

    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void SetUIActive(bool active)
    {
        UWAPlatform.UWAEngine.SetUIActive(active);
    }
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void AssetDumpInOverview()
    {
        UWAPlatform.UWAEngine.AssetDumpInOverview();
    }

    
    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void PushSample(string sampleName)
    {
        UWAPlatform.UWAEngine.PushSample(sampleName);
    }
    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void PopSample()
    {
        UWAPlatform.UWAEngine.PopSample();
    }

#if UNITY_2018_1_OR_NEWER
    
    
    
    
    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void Upload(Action<bool> callback, string user, string password, string projectName, int timeLimitS)
    {
        UWAPlatform.UWAEngine.Upload(callback, user, password, projectName, timeLimitS);
    }

    
    
    
    
    
    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void Upload(Action<bool> callback, string user, string password, int projectId, int timeLimitS)
    {
        UWAPlatform.UWAEngine.Upload(callback, user, password, projectId, timeLimitS);
    }
#endif

    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, float value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, int value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, Vector3 value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void LogValue(string valueName, bool value)
    {
        UWAPlatform.UWAEngine.LogValue(valueName, value);
    }
    [Conditional("ENABLE_PROFILER")]
    public static void AddMarker(string valueName)
    {
        UWAPlatform.UWAEngine.AddMarker(valueName);
    }

    
    
    
    
    [Conditional("ENABLE_PROFILER")]
    public static void SetOverrideLuaLib(string luaLib)
    {
#if !UNITY_IPHONE
        UWAPlatform.UWAEngine.SetOverrideLuaLib(luaLib);
#endif
    }
}