using UnityEngine;

using System.Collections;

[ExecuteInEditMode] 
public class Mirror : MonoBehaviour
{
    public enum NOARMLDIR{
        X = 1,
        Y = 2,
        Z = 3,
    };
    public bool m_DisablePixelLights = true;
    public int m_TextureSize = 256;
    public float m_ClipPlaneOffset = 0.07f;
	public bool m_IsFlatMirror = true;
    public NOARMLDIR m_normalDir = NOARMLDIR.Y; 
    public LayerMask m_ReflectLayers = -1;
    public Camera renderCamera;
    public static bool EnableByConfig = false;
    public int m_AA = 2;
    private float[] cullDistance = new float[32]; 
    
    
    
    public bool _noDiffuse = false;
       
    
   
    private RenderTexture m_ReflectionTexture = null;
    private int m_OldReflectionTextureSize = 0;
   
    private static bool s_InsideRendering = false;

    protected static Camera _reflectionCamera;
    private MaterialPropertyBlock m_block;
    private Renderer m_renderer;

    public Vector3 NoramlDir
    {
        get
        {
            if(m_normalDir == NOARMLDIR.Y)
            {
                return transform.up;
            }
            else if(m_normalDir == NOARMLDIR.Z)
            {
                return transform.forward;
            }
            else if(m_normalDir == NOARMLDIR.X)
            {
                return transform.right;
            }
            else
            {
                return transform.up;
            }
        }
    }

    void Start()
    {
        m_renderer = GetComponent<Renderer>();
    }

    private void ClearRefTexture()
    {
        if( m_ReflectionTexture ) {
            DestroyImmediate( m_ReflectionTexture );
            m_ReflectionTexture = null;
        }     
    }
    
    public void LateUpdate()
    {
        if( !enabled || !m_renderer || !m_renderer.sharedMaterial || !m_renderer.enabled )
            return;
        if (!EnableByConfig)
        {
            ClearRefTexture();
            return;
        }
        Camera curRenderCam = CameraManager.GetFirstCamera();
        if (!curRenderCam && renderCamera == null)
            return;
        Camera cam = renderCamera == null ? curRenderCam : renderCamera;

        if (m_block == null)
        {
            m_block = new MaterialPropertyBlock();
            m_renderer = GetComponent<Renderer>();
        }
        if( s_InsideRendering )
            return;
        s_InsideRendering = true;
       
        
        CreateMirrorObjects(cam, ref _reflectionCamera);
       
        Vector3 pos = transform.position;
		Vector3 normal;
		if(m_IsFlatMirror){
            normal = NoramlDir;
		}
		else{ 
			normal= transform.position - cam.transform.position ;
			normal.Normalize();
		}
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if( m_DisablePixelLights )
            QualitySettings.pixelLightCount = 0;
       
        
        _reflectionCamera.CopyFrom(cam);
        cullDistance = cam.layerCullDistances;
       
        
           
            
                
           
           
           
           
           
           
              
           
        
        cullDistance[23] = cullDistance[23] + 690;
        _reflectionCamera.layerCullDistances = cullDistance;
        _reflectionCamera.allowHDR = false;
        _reflectionCamera.allowMSAA = false;
        _reflectionCamera.layerCullSpherical = true;
        float d = -Vector3.Dot (normal, pos) - m_ClipPlaneOffset;
        Vector4 reflectionPlane = new Vector4 (normal.x, normal.y, normal.z, d);
   
        Matrix4x4 reflection = Matrix4x4.zero;
        CalculateReflectionMatrix (ref reflection, reflectionPlane);
        Vector3 oldpos = cam.transform.position;
        Vector3 newpos = reflection.MultiplyPoint( oldpos );
        _reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;


        Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, pos, normal, 1.0f);
        Matrix4x4 projection = cam.projectionMatrix;
        CalculateObliqueMatrix (ref projection, clipPlane);
        _reflectionCamera.projectionMatrix = projection;

        _reflectionCamera.cullingMask = m_ReflectLayers.value;
        _reflectionCamera.targetTexture = m_ReflectionTexture;
        GL.SetRevertBackfacing (true);
        _reflectionCamera.transform.position = newpos;
        Vector3 euler = cam.transform.eulerAngles;
        _reflectionCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
        _reflectionCamera.Render();
        _reflectionCamera.transform.position = oldpos;
        GL.SetRevertBackfacing (false);
        m_block.SetTexture("_Ref", m_ReflectionTexture);
        m_renderer.SetPropertyBlock(m_block);

        if( m_DisablePixelLights )
            QualitySettings.pixelLightCount = oldPixelLightCount;
       
        s_InsideRendering = false;
    }
   
    void OnDisable()
    {
        ClearRefTexture();



    }
   
   
    private void UpdateCameraModes( Camera src, Camera dest )
    {
        if( dest == null )
            return;

        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;

            if (_noDiffuse == true)
            {
                dest.enabled = false;
                dest.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                dest.backgroundColor = Color.black;
                if (!sky || !sky.material)
                {
                    mysky.enabled = false;
                }
                else
                {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
            }
        }

        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
		dest.renderingPath = src.renderingPath;
    }
   

    private void CreateMirrorObjects( Camera currentCamera, ref Camera reflectionCamera )
    {
        if( !m_ReflectionTexture || m_OldReflectionTextureSize != m_TextureSize )
        {
            if( m_ReflectionTexture )
                DestroyImmediate( m_ReflectionTexture );
            
            if (_noDiffuse)
                m_ReflectionTexture = new RenderTexture( m_TextureSize, m_TextureSize, 16 ,  RenderTextureFormat.ARGB32);
            else
                m_ReflectionTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);

            m_ReflectionTexture.name = "__MirrorReflection" + GetInstanceID();
            m_ReflectionTexture.isPowerOfTwo = true;
            m_ReflectionTexture.antiAliasing = m_AA;
            m_ReflectionTexture.hideFlags = HideFlags.DontSave;
            m_OldReflectionTextureSize = m_TextureSize;
        }
       

        
        if (!reflectionCamera) 
        {
            GameObject go = new GameObject( "Mirror Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox) );
            reflectionCamera = go.GetComponent<Camera>();
            reflectionCamera.enabled = false;
            reflectionCamera.transform.position = transform.position;
            reflectionCamera.transform.rotation = transform.rotation;
            reflectionCamera.gameObject.AddComponent<FlareLayer>();
            reflectionCamera.useOcclusionCulling = false;
            go.hideFlags = HideFlags.HideAndDontSave;
            
        }       
    }
   
    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }
   
    private Vector4 CameraSpacePlane (Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * m_ClipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint( offsetPos );
        Vector3 cnormal = m.MultiplyVector( normal ).normalized * sideSign;
        return new Vector4( cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos,cnormal) );
    }
   
    private static void CalculateObliqueMatrix (ref Matrix4x4 projection, Vector4 clipPlane)
    {
        Vector4 q = projection.inverse * new Vector4(
            sgn(clipPlane.x),
            sgn(clipPlane.y),
            1.0f,
            1.0f
        );
        Vector4 c = clipPlane * (2.0F / (Vector4.Dot (clipPlane, q)));

        projection[2] = c.x - projection[3];
        projection[6] = c.y - projection[7];
        projection[10] = c.z - projection[11];
        projection[14] = c.w - projection[15];
    }

    private static void CalculateReflectionMatrix (ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F*plane[0]*plane[0]);
        reflectionMat.m01 = (   - 2F*plane[0]*plane[1]);
        reflectionMat.m02 = (   - 2F*plane[0]*plane[2]);
        reflectionMat.m03 = (   - 2F*plane[3]*plane[0]);

        reflectionMat.m10 = (   - 2F*plane[1]*plane[0]);
        reflectionMat.m11 = (1F - 2F*plane[1]*plane[1]);
        reflectionMat.m12 = (   - 2F*plane[1]*plane[2]);
        reflectionMat.m13 = (   - 2F*plane[3]*plane[1]);
   
        reflectionMat.m20 = (   - 2F*plane[2]*plane[0]);
        reflectionMat.m21 = (   - 2F*plane[2]*plane[1]);
        reflectionMat.m22 = (1F - 2F*plane[2]*plane[2]);
        reflectionMat.m23 = (   - 2F*plane[3]*plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}
