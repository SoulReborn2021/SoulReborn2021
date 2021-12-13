
#define ENABLE_STAT
using System;
using UnityEngine;
using System.Linq;
using System.IO;
#if USING_LUA_CONFIG
using XElement = XMLEngine.GameEngine.Logic.LuaElement;
using XAttribute = XMLEngine.GameEngine.Logic.LuaElement;
#else
using System.Xml;
using System.Xml.Linq;
#endif
using System.Collections;
using System.Collections.Generic;
using XMLEngine.GameEngine.Logic;
using XMLEngine.GameEngine.SilverLight;
using XMLEngine.GameEngine.Data;
using XMLEngine.GameEngine.Network;
using XMLEngine.GameEngine.Network.Tools;
using XMLEngine.GameFramework.Logic;
using XMLEngine.GameEngine.Render;
using XMLEngine.GameEngine.Common;
using XMLEngine.JavaPlugins;
using System.Text;
using XML.Client.Interface;
using XMLGame.Framework.Logic;
using XMLEngine.GameEngine.Encrypt;
using XML.PlatSDK;
using GupInstanceGrass;
using LogCollection;
using UnityEngine.Android;
using UnityEngine.UI;



public class MainGame : TTMonoBehaviour
{
    
    
    
    public StageSL Stage = null;
    
    
    
    public GameObject ModalLayer = null;
    
    
    
    public GameObject DialogLayer = null;
    
    
    
    public GameObject NetWaiting = null;
    
    
    
    public UIJoystick Joystick = null;
    
    
    
    public NetAudioSource BackgroundAudio4UI = null;
    
    
    
    public AudioListener AudioListener4UI = null;
    
    
    
    public NetAudioSource BackgroundAudio43D = null;
    
    
    
    public AudioListener AudioListener43D = null;

    
    
    
    public Camera MainCamera = null;
    
    
    
    public Camera UICamera = null;
    
    
    
    public Transform UICameraAngle = null;

    public GameObject htmlLabelTempalte = null;
    
    
    
    
    
    
    
    
    
    
    
    public Light DirectLight = null;
    
    
    
    public NetAudioSource GlobalUIAudioSource = null;

    public UIAudioManager GlobalUIAudioManager = null;
    
    
    
    public GameObject UMeng = null;
    
    
    
    public StageSL Stage3D = null;
    
    
    
    public Camera UICamera3D = null;

    public LoadingMap LoadingMap = null;

    
    public int statisticsTimeCount = 300;
    
    public int skipAutoChangeGraphicCount = 3;
    
    public float lowerThreshold = 20;
    
    public float highterThreshold = 45;

    public GrassInstance Grassinstance = null;
    
    
    
    private bool InitOK = false;
    
    
    
    private byte[] TestBytes = null;
    public static int ticks;
    public static bool IsUpdateEnabled { get; private set; }
    private int rotMatrixID;
    private int roughnessLUTID;
    private Texture2D roughnessLUT;


    bool isDebug = false;

    
    
    
    void Awake()     
    {
        
        CDNLang.GetLoaclLang();
        
        UpdateUtil.CheckAssets();
        VersionConf.LoadVersionInApp();
        
        VersionConf.LoadVersion();
        
        DevGameConfig.GetGameConfig();
        
        if (LC_Log.Instance.IsLCEnabled())
        {
            LC_Log.Instance.Init();
        }
        
        if(DevGameConfig.GetConf(DevGameConfig.GameConf.combineAB))
            ABIndex.Init();
        Global.updateLua.Init(true);
        
        
        
#if ENABLE_UPDATE
        IsUpdateEnabled = true;
#else
        IsUpdateEnabled = false;
#endif
        XMLLog.DebugLog = UnityDebugTextLog.DebugLog;
        ticks = Global.GetMyTimer();
        _current = this;
        Application.runInBackground = true;
        
        DeviceInfo.Init();

        
        
        
        
        Application.lowMemory += LowMemoryCallBack;
    }

    
    void Start()
    {     
        
        Global.setIOSNoBackup();
        
        InitStep1();
        
        InitStep2();
        RaptorJava.Cpumemcrash();
#if UNITY_EDITOR
        RaptorJava.___();
#endif
        AssetBundleLoaderPool.GetInstance().Init();

        StartGame();
    }

    void StartGame()
    {
        
        
        
#if UNITY_IOS
        Super.ShowNetWaiting();
#elif UNITY_ANDROID

#if _TAIWAN
        Super.ShowNetWaiting();
#endif

#endif

        HeroBDCControl.GetInstance().open_game();
        
        InitAdvertion();
        
        XML_ABManager.GetInstance().LoadABConf();
        StartCoroutine(ShowSplash());

    }

    private void InitAdvertion()
    {
#if _NETMARBLE









        string TranformName = "/UI Root (2D)/Camera/Anchor/StageSL/SingularSDKObject";
        GameObject targetgo = GameObject.Find(TranformName);
        if (targetgo != null)
        {
            targetgo.AddComponent<SingularSDK>();
        }
#endif
    }

    public IEnumerator ShowSplash()
    {
        HeroBDCControl.GetInstance().game_first_page();

        if (XMLPlatSDKDefine.GetPlatTypeWithMarco() == XMLPlatSDKDefine.platform_Type.Japan)
        {
            yield return new WaitForSeconds(0.5f);
            if (_SplashWnd == null)
            {
                _SplashWnd = U3DUtils.NEW<JP_login>();
                _SplashWnd.SetBackgroundURL();
                Global.MainStage3D.Children.Add(_SplashWnd);
            }

            XMLDebug.Log("LoadUpdateConfig 1111111111111111111111111:  " + Time.time );
            yield return new WaitForSeconds(JP_login.MEDIA_TIME_INTERVAL);
            DestroySplashWnd();
        }
        else
        {
            XMLDebug.Log("LoadUpdateConfig 22222222222222222222222222");
        }
#if _KOREA
        KoreaS.SDKInterface.KoreaSDKMgr.GetInstance().InitAdjust();
#elif _JAPAN
        JapanS.SDKInterface.JapanSDKMgr.GetInstance().InitAdjust();
#endif
        LoadUpdateConfig();
    }

    public static void DestroySplashWnd()
    {
        if (_SplashWnd != null)
        {
            GameObject.Destroy(_SplashWnd.gameObject);
            _SplashWnd = null;

            XMLDebug.Log("DestroySplashWnd!!!!!!!!!!!!!!!!!!!!!");
        }
    }

    private static JP_login _SplashWnd = null;
    public void LoadUpdateConfig()
    {
        Global.MainStage = Stage;    
        
#if UNITY_EDITOR || (_TAIWAN && UNITY_IOS)        
        CDNGameConfig.GetInstance().InitFromFile();
#endif
        bool suc = UpdateEngine.instance.LoadLocalVersionXML();
        if (suc)
        {
            Super.ShowCheckingUpdatePack(Global.MainStage3D);
#if _TAIWAN && UNITY_IOS
            StartCoroutine(CDNGameConfig.GetInstance().GetCDNCtrlInfo(
                () =>
                {
                    UpdateUtil.ProcessAssetsBeforeStartGame();
                }
            ));
#else
#if UNITY_ANDROID && _KOREA
    if(KoreaS.SDKInterface.KoreaSDKMgr.GetInstance().getPlayerPrefsData() == 0)
    {
        KoreaS.SDKInterface.KoreaSDKMgr.GetInstance().RequestPermissionBox();
    }else if(KoreaS.SDKInterface.KoreaSDKMgr.GetInstance().getPlayerPrefsData() > 0)
    {
        UpdateUtil.ProcessAssetsBeforeStartGame();
    }

#else
    UpdateUtil.ProcessAssetsBeforeStartGame();
#endif

            
#endif
            InitOK = true;
        }
    }

    public void StartInitGameCo()
    {
        
#if UNITY_IOS
        Super.HideNetWaiting();
#endif
        StartCoroutine(InitGame());
    }

    public IEnumerator InitGame()
    {
        DevGameConfig.InitVoiceLang();
        XMLPlatSDKManager.InitSDK();
        
        XML_ABManager.GetInstance().LoadABConf();
        yield return "";
#if UWA && UNITY_ANDROID
        Debug.Log("UWAEngine Init");
        UWAEngine.StaticInit();          
        GameObject uwaGo = GameObject.Find("UWA_Launcher");
        if(uwaGo != null)
        {
            if(uwaGo.GetComponent<DontUnloadObject>() == null)
                uwaGo.AddComponent<DontUnloadObject>(); 
            uwaGo.layer = LayerMask.NameToLayer("UI2D");          
        }
        else
        {
            Debug.LogError("UWA init failed!");
        }
#endif
        if (Global.gameLua.IsInited)
        {
            yield break;
        }
        Global.xmldic.Clear();
        Global.gameLua.Init(false);
        
        Global.SetLogInfo(XMLDebug.IsOpenDebug, XMLDebug.logLevel);
        

        GameObject ui3d = GameObject.Find("UI Root (3D)");
        MyLuaBehaviour globalParams = ui3d.GetComponent<MyLuaBehaviour>();
        if (globalParams != null)
        {
            globalParams.Init();
        }
        
        var luaUpdater = ui3d.GetComponent<LuaUpdater>();
        if (luaUpdater == null) luaUpdater = ui3d.AddComponent<LuaUpdater>();
        luaUpdater.OnInit(Global.gameLua);

#if UNITY_EDITOR
        Super.DestroyCheckingUpdatePack();
#endif        
        EventSystem.Instance.PushEvent("GE_APPLICATION_INITED");
        yield return "";
        
        if (globalParams != null)
        {
            globalParams.TriggerAcion(XMLGame.Framework.Lua.BehaviourAction.Start);
            globalParams.TriggerAcion(XMLGame.Framework.Lua.BehaviourAction.OnEnable);
        }
        

        

        SettingManager.Init();
        SettingManager.LoadInfo();
        SettingManager.InstallChange();
        SettingManager.InstallContrastSaturation();

        XMLDebug.Log("intercept is:" + CDNGameConfig.GetInstance().cdnClientConf.intercept);
        if (CDNGameConfig.GetInstance().cdnClientConf.intercept && SettingManager.IsLowLevelDevice())
        {
            if (Global.IsSimulator())
            {
                
                int memSize = SystemInformation.GetMemorySize();
                if (memSize < 2000)
                {
                    Super.ShowMessageBoxPartForCSharp(Global.MainStage3D, 0, Global.GetLangUpdate("update_00005"), UpdateEngine.GetUpdateStr(0), null,
                        Global.GetLangUpdate("update_00021"), (s0) =>
                        {
                            if (s0 == 0)
                            {
                                StartCoroutine(InitializationDelay());
                            }
                        });
                }
                else
                {
                    StartCoroutine(InitializationDelay());
                }
            }
            else
            {
                Super.ShowMessageBoxPartForCSharp(Global.MainStage3D, 0, Global.GetLangUpdate("update_00005), UpdateEngine.GetUpdateStr(1), null,
                    UpdateEngine.GetUpdateStr(2), (s0) =>
                    {
                        if (s0 == 0)
                        {
#if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
#else
                                        
                XMLPlatSDKInterface.DirectExit();
#endif
                        }
                    });
            }

        }
        else
        {
#if UNITY_ANDROID
            StartCoroutine(InitializationDelay());
#else
            Initialization();
#endif
        }

        yield return true;
    }

    IEnumerator InitializationDelay()
    {
        InitOK = true;
        yield return new WaitForSeconds(0.05f);
        EventSystem.Instance.PushEvent("GE_APPLICATION_LOAD");
    }

    void Initialization()
    {
        InitOK = true;
        EventSystem.Instance.PushEvent("GE_APPLICATION_LOAD");
    }
    
    
    
    void InitStep1()
    {
        
        
        Global.GlobalMainWindow = Stage;
        Global.Joystick = Joystick;
        Global.BackgroundAudio4UI = BackgroundAudio4UI;
        Global.AudioListener4UI = AudioListener4UI;
        Global.BackgroundAudio43D = BackgroundAudio4UI;
        Global.AudioListener43D = AudioListener43D;
        Global.DirectLight = DirectLight;
        Global.MainStage3D = Stage3D;
        Global.Grassinstance = Grassinstance;
      

        HTMLEngine.NGUI.NGUIFont.LabelTemplate = htmlLabelTempalte;
        
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        
        InitData();

        
        XMLDate.Init();

        
        Global.MainCamera = MainCamera;
        Global.UICamera = UICamera;
        Global.UICameraAngles = UICameraAngle;
        Global.UICamera3D = UICamera3D;
        Global.MainCameraCtrl = MainCamera.GetComponent<CameraController>();
        UIGraphicRaycaster.SetEventCamera(MainCamera);
        Global.beforeEnterGame = true;
        
        if(Global.IsSimulator()) 
        { 
            UICamera3D.GetComponent<UICamera>().useKeyboard = false;
        }

        
        
        

        Global.ShaderPropertyID.Initialize();
        SettingManager.SetMainLOD(400);
    }

    
    
    
    void InitStep2()
    {
        Super.ModalLayer = ModalLayer;
        Super.DialogLayer = DialogLayer;
        Super.NetWaiting = NetWaiting;
        Super.GData.GlobalUIAudioSource = GlobalUIAudioSource;
        Super.GData.GlobalUIAudioManager = GlobalUIAudioManager;
        Super.MainWindowRoot = Stage;
        Super.MainWindowRoot3D = Stage3D;
        Super.CurrentLoadingMap = LoadingMap;
        LuaWindow.enableEventHandler = OnLuaWindowEnabled;
    }

    
    
    
    private void InitData()
    {
        Global.Data = new GData();
        Super.GData = new SuperData();
    }

    
    
    
    
    
    
    
    
    
    

    
    
    
    private int exitCount = 0;

    
    
    
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Home))
        {
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            
              
            
        }
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            Super.AutoBackWindow();
        }
        if (!InitOK)
        {
            return;
        }

        try
        {
#if !UNITY_EDITOR
            Global.UpdateFrameRate();
#endif
            RenderGame();
            Global.gameLua.GC();
        }
        catch (System.Exception ex)
        {
            XMLDebug.LogException(ex);
        }
    }

    
    
    
    void OnApplicationQuit()
    {
#if UNITY_IPHONE
        
		
		
#endif

        
        if (null != PlayZone.GlobalPlayZone)
        {
            PlayZone.GlobalPlayZone.CloseSocket();
        }

        PlatSDKMgr.OnAppQuit();
        XMLDebug.Dispose();
    }

    void OnApplicationFocus(bool bFocus)
    {
        XMLDebug.Log("OnApplicationFocus:" + bFocus);
#if UNITY_IOS
        Debug.Log("OnApplicationFocus:" + bFocus);
        if(bFocus)
        {
            
            MiniAppUtil.PauseAndResume();
        }
#endif 
        if (!bFocus)
        {
#if _HERO && UNITY_ANDROID
            
#endif
        }
        else
        {
#if _HERO && UNITY_ANDROID
            LocalNotificationInterface.Instance.Clean();
#endif
            
        }
    }

    private long lastPauseTimer;
    void OnApplicationPause(bool pauseStatus)
    {
        XMLDebug.Log("OnApplicationPause:" + pauseStatus);
        if (pauseStatus)
        {
            lastPauseTimer = Global.GetCorrectLocalTime(false);




        }
        else
        {
            if (Global.Data != null && lastPauseTimer != 0) 
            {
                Global.Data.ApplicationPauseTimer = Global.GetCorrectLocalTime(false) - lastPauseTimer;
                lastPauseTimer = 0;

                if ( !GameInstance.Game.ActiveDisconnect && (Global.Data.ApplicationPauseTimer >300000) )
                {
                    GameInstance.Game.PingTimeOut();
                }
            }

            
#if _NETMARBLE
            if (Global.Data != null && Global.Data.PlayGame)
            {
                UpdateEngine.instance.CheckUpdate(null, UpdateEngine.updateType.InGame);
            }
#endif
        }
#if UNITY_IPHONE










#endif
    }


    

    public int ignorIdx = -1;
    
    
    
    private void RenderGame()
    {
        if (ScreenShootTool.Instance().isPause)
            return;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        
#endif
        
        
        

        
        DoQueueMainActions();

        
        

        
        

        
        DispatcherTimerDriver.ExecuteTimers();

        
        EventSystem.Instance.Tick();

        
        XML.PlatSDK.XMLPlatSDKManager.Tick();

        
        if (null == Global.Data)
            return;

        if (Global.Data.WaitingForMapChange != 0)
            return;

        
        StoryBoard.runStoryBoards(false);

        
        bool isLeaderMoving = false;

        if (null != Super.MainGameMgr)
        {
            
            Super.MainGameMgr.onRenderScene();

            
            Super.MainGameMgr.onUIRenderFrame();
        }

        
        RenderManager.ProcessRenderObject(isLeaderMoving, false);

        notReachableDisConect();
    }

    
    
    
    

#region 

    public static MainGame _current = null;

    private List<Action> _actions = new List<Action>();

    public struct DelayedQueueItem
    {
        public float time;
        public Action action;
    }

    private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();
    List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

    public static void QueueOnMainThread(Action action)
    {
        QueueOnMainThread(action, 0f);
    }

    public static void QueueOnMainThread(Action action, float time)
    {
        if (time != 0)
        {
            lock (_current._delayed)
            {
                _current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action });
            }
        }
        else
        {
            lock (_current._actions)
            {
                _current._actions.Add(action);
            }
        }
    }

    List<Action> _currentActions = new List<Action>();
    private void DoQueueMainActions()
    {
        lock (_actions)
        {
            if (_actions.Count > 0)
            {
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
        }
        if (_currentActions.Count > 0)
        {
            for (int i = 0; i < _currentActions.Count; i++)
            {
                Action a = _currentActions[i];
                try
                {
                    a();
                }
                catch (System.Exception ex)
                {

                    XMLDebug.LogException(ex);
                }
            }
            _currentActions.Clear();
        }

        lock (_delayed)
        {
            if (_delayed.Count > 0)
            {
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));

                for (int i = 0; i < _currentDelayed.Count; i++)
                {
                    _delayed.Remove(_currentDelayed[i]);
                }
            }
        }

        if (_currentDelayed.Count > 0)
        {
            for (int i = 0; i < _currentDelayed.Count; i++)
            {
                DelayedQueueItem delayed = _currentDelayed[i];

                try
                {
                    delayed.action();
                }

                catch (System.Exception ex)
                {
                    XMLDebug.LogException(ex);
                }
            }

            _currentDelayed.Clear();
        }
    }

#endregion

    
    
    
    private float CheckNetworkRate = 0.2f;
    private float CheckValue = 0;
    public void StartCheckHasNetwork()
    {
#if UNITY_ANDROID
        CheckValue = 0;
        if (_current != null)
        {
            _current.InvokeRepeating("CheckHasNetwork" , CheckNetworkRate , CheckNetworkRate);
        }
#endif
    }

    public void CancelCheckHasNetwork()
    {
#if UNITY_ANDROID
        CheckValue = 0;
        if (_current != null)
        {
            _current.CancelInvoke("CheckHasNetwork");
        }
#endif
    }

    

    public void CheckHasNetwork()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            CheckValue += 1;
        }
        else
        {
            CheckValue = 0;
        }

        if (CheckValue > 5)  
        {
            CancelCheckHasNetwork();
            XMLDebug.Log("CheckHasNetwork == NetworkReachability.NotReachable");
            GChildWindow messageBoxWindow = Super.ShowMessageBox(Super.MainWindowRoot, 0, Global.GetLang, Global.GetLang,
                null, null, (messageBoxReturn) =>
            {
                
                
                
                XMLPlatSDKInterface.DirectExit();
                
            });
            
        }
    }

    void notReachableDisConect() 
    {
#if UNITY_IOS
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if ( !GameInstance.Game.ActiveDisconnect )
            {
                GameInstance.Game.PingTimeOut();
            }
        }
#elif UNITY_ANDROID

#endif

    }

    #region
    
    
    
    
    public void QuickClient(int second)
    {
        if (second <= 0)
        {
            
            XMLPlatSDKInterface.DirectExit();
            return;
        }
        else
        {
            
            Invoke("Quick_Client", second);
        }

    }
    
    
    
    public void Quick_Client()
    {
        
        XMLPlatSDKInterface.DirectExit();
    }
#endregion

    enum RenderQuality
    {
        High,
        Low
    }
    RenderQuality mRenderQuality = RenderQuality.High;
    float mAvgFrameRate = 45f;
    float mFrameWeight = 1f / 60f;
    void UpdateRenderQuality()
    {
        mAvgFrameRate = mAvgFrameRate * (1f - mFrameWeight) + mFrameWeight / Time.deltaTime;
        if (mAvgFrameRate < 20f && mRenderQuality == RenderQuality.High)
        {
            Shader.globalMaximumLOD = 1000;
            mRenderQuality = RenderQuality.Low;
        }
        else if (mAvgFrameRate > 25f && mRenderQuality == RenderQuality.Low)
        {
            Shader.globalMaximumLOD = 2000;
            mRenderQuality = RenderQuality.High;
        }
    }


#region
    
    
    
    private int m_StepServerTimesceond = 20;
    
    
    
    private GChildWindow m_GChildWindow_KuaFu = null;
    
    
    
    public void ShowStepServerTimeWaiting(int sceond)
    {
        m_StepServerTimesceond = sceond;
        if (IsInvoking("ShowMessgae_StepServer"))
            CancelInvoke("ShowMessgae_StepServer");
        Super.ShowNetWaiting();
        ShowMessgae_StepServer();
        InvokeRepeating("ShowMessgae_StepServer", 1.0f, 1.0f);
    }
    
    
    
    void ShowMessgae_StepServer()
    {
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        

        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
        
    }
#endregion

#region
    public void SaveShotPicture(string fileName)
    {
        
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif
        
        SavePicToGallery.SaveFinished += SavePictureFinished;
        StartCoroutine(SavePicToGallery.Save(fileName));
    }
    public void SavePictureFinished(bool sucess)
    {
        SavePicToGallery.SaveFinished -= SavePictureFinished;
        EventSystem.Instance.PushEvent("GE_SAVE_PICTURE_RESULT", sucess);
    }
#endregion

    void UpdateDistortEff()
    {
        
    }
    
    
    
    
    
    bool OnLuaWindowEnabled(object sender, BaseEventArgs args)
    {
        
        
        
        
        

        if (args.type == 1)
        {
            
            
            if (args.IDType == 1)
                Global.Data.openBloomUICount++;
            if (args.X == 1)
                Global.Data.openHDRUICount++;
            if (args.Y == 1)
                Global.Data.openAntiAliasingCount++;
            if (args.Z == 1)
                Global.Data.openTvMaskCount++;
        }
        else if (args.type == 0)
        {
            if (args.IDType == 1)
                Global.Data.openBloomUICount--;
            if(args.X == 1)
                Global.Data.openHDRUICount--;
            if (args.Y == 1)
                Global.Data.openAntiAliasingCount--;
            if (args.Z == 1)
                Global.Data.openTvMaskCount++;

            
            
        }
        Global.Data.openBloomUI = Global.Data.openBloomUICount > 0;
        Global.Data.openHDRUI = Global.Data.openHDRUICount > 0;
        Global.Data.openAntiAliasing = Global.Data.openAntiAliasingCount > 0;
        Global.Data.openTvMask = Global.Data.openTvMaskCount > 0;
        
        return true;
    }

    void LowMemoryCallBack()
    {
        XMLDebug.LogWarning("LowMemoryWarining!");
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }
}
