using System;
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

namespace MMT
{
    public class MobileMovieTexture : MonoBehaviour
    {
        #region Types

        public delegate void OnFinished(MobileMovieTexture sender);

        #endregion

        #region Editor Variables

        
        
        
#if UNITY_EDITOR
#if UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_5 || UNITY_4_6
		[StreamingAssetsLinkAttribute(typeof(MovieTexture), "Movie")]
#else
        [StreamingAssetsLinkAttribute(typeof(UnityEngine.Object), "Movie")]
#endif
#endif
		[SerializeField]
        private string m_path;

        
        
        
        [SerializeField]
        private Material[] m_movieMaterials;

        
        
        
        [SerializeField]
        private bool m_playAutomatically = true;

        
        
        
        [SerializeField]
        private bool m_advance = true;

        
        
        
        [SerializeField]
        private int m_loopCount = -1;

        
        
        
        [SerializeField]
        private float m_playSpeed = 1.0f;

        
        
        
        [SerializeField]
        private bool m_scanDuration = true;

        
        
        
        [SerializeField]
        private bool m_seekKeyFrame = false;

        #endregion

        #region Other Variables

        private IntPtr m_nativeContext = IntPtr.Zero;
        private IntPtr m_nativeTextureContext = IntPtr.Zero;

        private int m_picX = 0;
        private int m_picY = 0;

        private int m_yStride = 0;
        private int m_yHeight = 0;
        private int m_uvStride = 0;
        private int m_uvHeight = 0;

        private Vector2 m_uvYScale;
        private Vector2 m_uvYOffset;
        
        private Vector2 m_uvCrCbScale;
        private Vector2 m_uvCrCbOffset;

        private const int CHANNELS = 3; 
        private Texture2D[] m_ChannelTextures = new Texture2D[CHANNELS];

        private double m_elapsedTime;

        private bool m_hasFinished = true;

        public MobileMovieTexture()
        {
            Height = 0;
            Width = 0;
        }

        #endregion

        
        
        
        public event OnFinished onFinished;

        #region Properties

        
        
        
        public string Path { get { return m_path; } set { m_path = value; } }

        
        
        
		public bool AbsolutePath { get; set; }

        
        
        
        public Material[] MovieMaterial { get { return m_movieMaterials; } }

        
        
        
        public bool PlayAutomatically { set { m_playAutomatically = value; } }

        
        
        
        public int LoopCount { get { return m_loopCount; } set { m_loopCount = value; } }

        
        
        
        public float PlaySpeed { get { return m_playSpeed; } set { m_playSpeed = value; } }

        
        
        
        public bool ScanDuration { get { return m_scanDuration; } set { m_scanDuration = value; } }

        
        
        
        public bool SeekKeyFrame { get { return m_seekKeyFrame; } set { m_seekKeyFrame = value; } }

        
        
        
        public int Width { get; private set; }

        
        
        
        public int Height { get; private set; }

        
        
        
        public float AspectRatio
        {
            get
            {
                if (m_nativeContext != IntPtr.Zero)
                {
                    return GetAspectRatio(m_nativeContext);
                }
                else
                {
                    return 1.0f;
                }
            }
        }

        
        
        
        public double FPS
        {
            get
            {
                if (m_nativeContext != IntPtr.Zero)
                {
                    return GetVideoFPS(m_nativeContext);
                }
                else
                {
                    return 1.0;
                }
            }
        }

        
        
        
        public bool IsPlaying
        {
            get { return m_nativeContext != IntPtr.Zero && !m_hasFinished && m_advance; }
        }

        public bool Pause { get { return !m_advance; } set { m_advance = !value; } }

        
        
        
        public double PlayPosition
        {
            get { return m_elapsedTime; }
            set 
            {
                if (m_nativeContext != IntPtr.Zero)
                {
                    m_elapsedTime = Seek(m_nativeContext, value, m_seekKeyFrame);
                }
            }
        }

        
        
        
        public double Duration
        {
            get { return m_nativeContext != IntPtr.Zero ? GetDuration(m_nativeContext) : 0.0; }
        }

        #endregion

        #region Native Interface

#if UNITY_IPHONE && !UNITY_EDITOR
    private const string PLATFORM_DLL = "__Internal";
#else
        private const string PLATFORM_DLL = "theorawrapper";
#endif
        [DllImport(PLATFORM_DLL)]
        private static extern IntPtr CreateContext();

        [DllImport(PLATFORM_DLL)]
        private static extern void DestroyContext(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern bool OpenStream(IntPtr context, string path, int offset, int size, bool pot, bool scanDuration, int maxSkipFrames);

        [DllImport(PLATFORM_DLL)]
        private static extern void CloseStream(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetPicWidth(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetPicHeight(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetPicX(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetPicY(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetYStride(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetYHeight(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetUVStride(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern int GetUVHeight(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern bool HasFinished(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern double GetDecodedFrameTime(IntPtr context);

		[DllImport(PLATFORM_DLL)]
		private static extern double GetUploadedFrameTime(IntPtr context);

		[DllImport(PLATFORM_DLL)]
		private static extern double GetTargetDecodeFrameTime(IntPtr context);
        
        [DllImport(PLATFORM_DLL)]
        private static extern void SetTargetDisplayDecodeTime(IntPtr context, double targetTime);

        [DllImport(PLATFORM_DLL)]
        private static extern double GetVideoFPS(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern float GetAspectRatio(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern double Seek(IntPtr context, double seconds, bool waitKeyFrame);

        [DllImport(PLATFORM_DLL)]
        private static extern double GetDuration(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern IntPtr GetNativeHandle(IntPtr context, int planeIndex);

        [DllImport(PLATFORM_DLL)]
        private static extern IntPtr GetNativeTextureContext(IntPtr context);

        [DllImport(PLATFORM_DLL)]
        private static extern void SetPostProcessingLevel(IntPtr context, int level);

        #endregion

        #region Behaviour Overrides
        void Awake()
        {
            m_nativeContext = CreateContext();

            if (m_nativeContext == IntPtr.Zero)
            {
                Debug.LogError("Unable to create Mobile Movie Texture native context");
                return;
            }
        }

        void Start()
        {
            

            if (m_playAutomatically)
            {
                Play();
            }
        }

        void OnDestroy()
        {
            DestroyTextures();
            DestroyContext(m_nativeContext);
        }

        void Update()
        {
            if (m_nativeContext != IntPtr.Zero && !m_hasFinished)
            {
                
                
                var textureContext = GetNativeTextureContext(m_nativeContext);

                if (textureContext != m_nativeTextureContext)
                {
                    DestroyTextures();
                    AllocateTexures();

                    m_nativeTextureContext = textureContext;
                }

                m_hasFinished = HasFinished(m_nativeContext);

                if (!m_hasFinished)
                {
                    if (m_advance)
                    {
                        m_elapsedTime += Time.deltaTime * Mathf.Max(m_playSpeed, 0.0f);
                    }
                }
                else
                {
                    if ((m_loopCount - 1) > 0 || m_loopCount == -1)
                    {
                        if (m_loopCount != -1)
                        {
                            m_loopCount--;
                        }

                        m_elapsedTime = m_elapsedTime % GetDecodedFrameTime(m_nativeContext);

                        Seek(m_nativeContext, 0, false);

                        m_hasFinished = false;
                    }
                    else if (onFinished != null)
                    {
						m_elapsedTime = GetDecodedFrameTime(m_nativeContext);

                        onFinished(this);
                    }

                }

                SetTargetDisplayDecodeTime(m_nativeContext, m_elapsedTime);

            }
        }


        #endregion

        #region Methods

        public void Play()
        {
            m_elapsedTime = 0.0;

            Open();

            m_hasFinished = false;

            
            if (MobileMovieManager.Instance == null)
            {
                gameObject.AddComponent<MobileMovieManager>();
            }
        }

        public void Stop()
        {
            CloseStream(m_nativeContext);
			m_hasFinished = true;
        }

        private void Open()
        {
            string path = m_path;
            long offset = 0;
            long length = 0;

            if (!AbsolutePath)
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                        path = Application.dataPath;

                        if (!AssetStream.GetZipFileOffsetLength(Application.dataPath, m_path, out offset, out length))
                        {
                            return;
                        }
                        break;
                    default:
                        path = Application.streamingAssetsPath + "/" + m_path;
                        break;
                }
            }


            
            const bool powerOf2Textures = false;

            
            const int maxSkipFrames = 16;

            if (m_nativeContext != IntPtr.Zero && OpenStream(m_nativeContext, path, (int)offset, (int)length, powerOf2Textures, m_scanDuration, maxSkipFrames))
            {
                Width = GetPicWidth(m_nativeContext);
                Height = GetPicHeight(m_nativeContext);

                m_picX = GetPicX(m_nativeContext);
                m_picY = GetPicY(m_nativeContext);

				m_yStride = GetYStride(m_nativeContext);
				m_yHeight = GetYHeight(m_nativeContext);
				m_uvStride = GetUVStride(m_nativeContext);
				m_uvHeight = GetUVHeight(m_nativeContext);

                CalculateUVScaleOffset();
            }
            else
            {
                Debug.LogError("Unable to open movie " + m_nativeContext, this);
            }
        }

        private void AllocateTexures()
        {
            m_ChannelTextures[0] = Texture2D.CreateExternalTexture(m_yStride, m_yHeight, TextureFormat.Alpha8, false, false, GetNativeHandle(m_nativeContext, 0));
            m_ChannelTextures[1] = Texture2D.CreateExternalTexture(m_uvStride, m_uvHeight, TextureFormat.Alpha8, false, false, GetNativeHandle(m_nativeContext, 1));
            m_ChannelTextures[2] = Texture2D.CreateExternalTexture(m_uvStride, m_uvHeight, TextureFormat.Alpha8, false, false, GetNativeHandle(m_nativeContext, 2));
            
            if (m_movieMaterials != null)
            {
                for (int i = 0; i < m_movieMaterials.Length; ++i)
                {
                    var mat = m_movieMaterials[i];

                    if (mat != null)
                    {
                        SetTextures(mat);
                    }
                }
            }
        }

        public void SetTextures(Material material)
        {
            material.SetTexture("_YTex", m_ChannelTextures[0]);
            material.SetTexture("_CbTex", m_ChannelTextures[1]);
			material.SetTexture("_CrTex", m_ChannelTextures[2]);

            material.SetTextureScale("_YTex", m_uvYScale);
            material.SetTextureOffset("_YTex", m_uvYOffset);

            material.SetTextureScale("_CbTex", m_uvCrCbScale);
            material.SetTextureOffset("_CbTex", m_uvCrCbOffset);
        }

        public void RemoveTextures(Material material)
        {
            material.SetTexture("_YTex", null);
			material.SetTexture("_CbTex", null);
            material.SetTexture("_CrTex", null);
        }

        private void CalculateUVScaleOffset()
        {
			var picWidth = (float)Width;
			var picHeight = (float)Height;
			var picX = (float)m_picX;
			var picY = (float)m_picY;
			var yStride = (float)m_yStride;
			var yHeight = (float)m_yHeight;
			var uvStride = (float)m_uvStride;
			var uvHeight = (float)m_uvHeight;



            m_uvYScale = new Vector2(picWidth / yStride, -(picHeight / yHeight));
            m_uvYOffset = new Vector2(picX / yStride, (picHeight + picY) / yHeight);

            m_uvCrCbScale = new Vector2();
            m_uvCrCbOffset = new Vector2();

            if (m_uvStride == m_yStride)
            {
                m_uvCrCbScale.x = m_uvYScale.x;
            }
            else
            {
                m_uvCrCbScale.x = (picWidth / 2.0f) / uvStride;
            }

            if (m_uvHeight == m_yHeight)
            {
                m_uvCrCbScale.y = m_uvYScale.y;
                m_uvCrCbOffset = m_uvYOffset;
            }
            else
            {
                m_uvCrCbScale.y = -((picHeight / 2.0f) / uvHeight);
                m_uvCrCbOffset = new Vector2((picX / 2.0f) / uvStride, (((picHeight + picY) / 2.0f) / uvHeight));
            }
        }

        private void DestroyTextures()
        {
            if (m_movieMaterials != null)
            {
                for (int i = 0; i < m_movieMaterials.Length; ++i)
                {
                    var mat = m_movieMaterials[i];

                    if (mat != null)
                    {
                        RemoveTextures(mat);
                    }
                }
            }

            for (int i = 0; i < CHANNELS; ++i)
            {
                if (m_ChannelTextures[i] != null)
                {
                    Destroy(m_ChannelTextures[i]);
                    m_ChannelTextures[i] = null;
                }
            }
        }

       
        #endregion
    }
}

