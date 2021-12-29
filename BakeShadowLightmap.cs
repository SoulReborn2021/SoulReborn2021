using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BakeShadowLightmap
{
    [MenuItem]
    static void Execute()
    {
        GameObject sceneRoot = GameObject.Find("scene");
        if (!sceneRoot)
        {
            Debug.LogError("Bake Failed: Scene Root not found! the scene root object should be named as 'Scene'");
            return;
        }

        GameObject mainLightObj = GameObject.FindGameObjectWithTag("MainLight");
        if (!mainLightObj)
        {
            Debug.LogError("Bake Failed: Main Directional Light need to be set to 'MainLight' tag");
            return;
        }
        
        LightmapData data = LightmapSettings.lightmaps[0];
        if (LightmapSettings.lightmaps.Length == 0)
        {
            Debug.LogError("Bake Failed: the scene must be baked already");
            return;
        }

        MeshRenderer[] renderers = sceneRoot.GetComponentsInChildren<MeshRenderer>();
        Texture2D posMap = new Texture2D(data.lightmapColor.width, data.lightmapColor.height, TextureFormat.RGB24, false);
        
        List<MeshRenderer> bakedRenderers = new List<MeshRenderer>();
        List<MeshRenderer> notbakedRenderers = new List<MeshRenderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            if (renderers[i].enabled && renderers[i].lightmapIndex >= 0 && 
			    renderers[i].sharedMaterial.renderQueue < 3000 && 
			    renderers[i].sharedMaterial.GetTag("Queue", true).CompareTo("Transparent") != 0)
                bakedRenderers.Add(renderers[i]);
            else
                notbakedRenderers.Add(renderers[i]);
        }

        
        EditorUtility.DisplayProgressBar("Bake Shadow", "Render Depth", 0f);
        for (int i = 0; i < notbakedRenderers.Count; ++i)
        {
            notbakedRenderers[i].enabled = false;
        }

        Bounds bounds = new Bounds();
        for (int i = 0; i < renderers.Length; ++i)
        {
            if (i == 0)
                bounds = new Bounds(renderers[i].bounds.center, renderers[i].bounds.size);
            else
                bounds.Encapsulate(renderers[i].bounds);
        }

        GameObject camObj = new GameObject("LightCamera");
        Camera lightCam = camObj.AddComponent<Camera>();
        lightCam.orthographic = true;
        lightCam.aspect = 1f;
        lightCam.backgroundColor = new Color(1f, 1f, 1f, 1f);
        lightCam.enabled = false;
        camObj.transform.rotation = mainLightObj.transform.rotation;
        Vector3 eular = camObj.transform.eulerAngles;
        eular.z = 0f;
        camObj.transform.eulerAngles = eular;
        camObj.transform.position = bounds.center;
        Vector3[] extends = new Vector3[] { bounds.extents, new Vector3(bounds.extents.x, bounds.extents.y, -bounds.extents.z), new Vector3(bounds.extents.x, -bounds.extents.y, bounds.extents.z) 
        , new Vector3(bounds.extents.x, -bounds.extents.y, -bounds.extents.z), new Vector3(-bounds.extents.x, bounds.extents.y, bounds.extents.z), new Vector3(-bounds.extents.x, bounds.extents.y, -bounds.extents.z)
        , new Vector3(-bounds.extents.x, -bounds.extents.y, bounds.extents.z), new Vector3(-bounds.extents.x, -bounds.extents.y, -bounds.extents.z)};
        Vector3[] localCorner = new Vector3[extends.Length];
        float[] localExtends = new float[extends.Length * 2];
        float[] localDepth = new float[extends.Length];
        for (int i = 0; i < localCorner.Length; ++i)
        {
            Vector3 corner = camObj.transform.InverseTransformDirection(extends[i]);
            localExtends[i * 2] = Mathf.Abs(corner.x);
            localExtends[i * 2 + 1] = Mathf.Abs(corner.y);
            localDepth[i] = Mathf.Abs(corner.z);
        }
        lightCam.orthographicSize = Mathf.Max(localExtends);
        lightCam.nearClipPlane = -Mathf.Max(localDepth);
        lightCam.farClipPlane = -lightCam.nearClipPlane;

        RenderTexture depthTexture = new RenderTexture(4096, 4096, 24, RenderTextureFormat.ARGB32);
        depthTexture.name = "BakeShadowLightmap depthTexture";
        lightCam.targetTexture = depthTexture;
        lightCam.RenderWithShader(Shader.Find("Hidden/RenderDepth"), null);
        lightCam.targetTexture = null;

        for (int i = 0; i < notbakedRenderers.Count; ++i)
        {
            notbakedRenderers[i].enabled = true;
        }

        
        float progress = 0.1f;
        int size = LightmapSettings.lightmaps[0].lightmapColor.width;
        EditorUtility.DisplayProgressBar("Bake Shadow", "Bake Textures", progress);
        Texture2D[] coordTextures = new Texture2D[LightmapSettings.lightmaps.Length];
        Texture2D[] depthTextures = new Texture2D[LightmapSettings.lightmaps.Length];
        Color[] white = new Color[4096 * 4096];
        for (int i = 0; i < white.Length; ++i)
        {
            white[i] = Color.white;
        }
        for (int i = 0; i < coordTextures.Length; ++i)
        {
            coordTextures[i] = new Texture2D(4096, 4096, TextureFormat.ARGB32, false, true);
            depthTextures[i] = new Texture2D(4096, 4096, TextureFormat.ARGB32, false, true);
            depthTextures[i].SetPixels(white);
        }
        float p = 0.85f / bakedRenderers.Count;
        for (int i = 0; i < bakedRenderers.Count; ++i)
        {
            WriteTexture(lightCam, coordTextures[bakedRenderers[i].lightmapIndex], depthTextures[bakedRenderers[i].lightmapIndex], bakedRenderers[i]);
            progress += p;
            EditorUtility.DisplayProgressBar("Bake Shadow", "Bake Textures", progress);
        }
        for (int i = 0; i < coordTextures.Length; ++i)
        {
            coordTextures[i].Apply();
            depthTextures[i].Apply();
        }
        GameObject.DestroyImmediate(camObj);

        
        EditorUtility.DisplayProgressBar("Bake Shadow", "Bake Shadowmaps", progress);
        p = 0.05f / coordTextures.Length;
        string[] outputPath = new string[coordTextures.Length];
        for (int i = 0; i < coordTextures.Length; ++i)
        {
            Material m = new Material(Shader.Find("Hidden/BakeShadow"));
            m.SetTexture("_LightTex", depthTexture);
            m.SetTexture("_CoordTex", coordTextures[i]);
            m.SetTexture("_DepthTex", depthTextures[i]);
            m.SetFloat("_ShadowStrength", mainLightObj.GetComponent<Light>().shadowStrength);
            m.SetFloat("_Bias", mainLightObj.GetComponent<Light>().shadowBias * 0.05f);
            RenderTexture rt = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            rt.name = "BakeShadowLightmap rt";
            Graphics.Blit(null, rt, m, 0);
            RenderTexture rt1 = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32);
            rt1.name = "BakeShadowLightmap rt1";
            Graphics.Blit(rt, rt1, m, 1);

            RenderTexture.active = rt1;
            Texture2D resultTex = new Texture2D(size, size, TextureFormat.ARGB32, true);
            resultTex.ReadPixels(new Rect(0, 0, size, size), 0, 0, true);
            resultTex.Apply();
            RenderTexture.ReleaseTemporary(rt);
            RenderTexture.ReleaseTemporary(rt1);
            string path = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapColor);
            path = path.Substring(0, path.LastIndexOf('.')) + "_shadow.jpg";
            outputPath[i] = path;
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                byte[] bytes = resultTex.EncodeToJPG();
                fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
            }
            progress += p;
            EditorUtility.DisplayProgressBar("Bake Shadow", "Bake Shadowmaps", progress);
        }

        EditorUtility.DisplayProgressBar("Bake Shadow", "Reimporting", progress);
        AssetDatabase.Refresh();
        for (int i = 0; i < outputPath.Length; ++i)
        {
            TextureImporter ti = TextureImporter.GetAtPath(outputPath[i]) as TextureImporter;
            ti.mipmapEnabled = false;
            ti.maxTextureSize = size;
            ti.textureFormat = TextureImporterFormat.AutomaticCompressed;
            ti.grayscaleToAlpha = false;
            ti.isReadable = false;
            ti.SaveAndReimport();
        }
        AssetDatabase.Refresh();

        ApplyBakedShadowMap();
        SetShadowMap settingscript = sceneRoot.GetComponent<SetShadowMap>();
        if (!settingscript)
            settingscript = sceneRoot.AddComponent<SetShadowMap>();
        Texture2D[] shadowmaps = new Texture2D[outputPath.Length];
        for (int i = 0; i < outputPath.Length; ++i)
        {
            shadowmaps[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath[i]);
        }
        settingscript.shadowMaps = shadowmaps;
        EditorUtility.ClearProgressBar();
    }

    [MenuItem]
    static void ApplyBakedShadowMap()
    {
        LightmapSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
        LightmapData[] lightmaps = new LightmapData[LightmapSettings.lightmaps.Length];
        for (int i = 0; i < lightmaps.Length; ++i)
        {
            string path = AssetDatabase.GetAssetPath(LightmapSettings.lightmaps[i].lightmapColor);
            path = path.Substring(0, path.LastIndexOf('.')) + "_shadow.jpg";
            LightmapSettings.lightmaps[i].lightmapDir = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            lightmaps[i] = new LightmapData() { lightmapColor = LightmapSettings.lightmaps[i].lightmapColor, lightmapDir = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
        }
        LightmapSettings.lightmaps = lightmaps;
    }

    [MenuItem("shader")]
    static void SetRealtimeShader()
    {
        GameObject g = GameObject.Find("scene");
        if (!g)
            return;

        Shader objShader = Shader.Find("Standard (Specular setup)");
        Shader terrainShader = Shader.Find("Artist/Scene/Terrain Bump Full");
        MeshRenderer[] renderers = g.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            MeshRenderer r = renderers[i];
            for (int j = 0; j < r.sharedMaterials.Length; ++j)
            {
				Material m = r.sharedMaterials[j];
				if (!m)
				{
					Debug.LogError("Missing Material", r);
					continue;
				}
				int queue = m.renderQueue;
                if (m.shader.name.CompareTo("Artist/Mobile Specular Standard") == 0)
                    m.shader = objShader;
                else if (m.shader.name.CompareTo("Artist/Scene/Terrain Bump Mobile") == 0)
                    m.shader = terrainShader;
				m.renderQueue = queue;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("shader")]
    static void SetBakedShader()
    {
        GameObject g = GameObject.Find("scene");
        if (!g)
            return;

        Shader objShader = Shader.Find("Artist/Mobile Specular Standard");
        Shader terrainShader = Shader.Find("Artist/Scene/Terrain Bump Mobile");
        MeshRenderer[] renderers = g.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; ++i)
        {
            MeshRenderer r = renderers[i];
            for (int j = 0; j < r.sharedMaterials.Length; ++j)
            {
				Material m = r.sharedMaterials[j];
				if (!m)
				{
					Debug.LogError("Missing Material", r);
					continue;
				}
				int queue = m.renderQueue;
                if (m.shader.name.CompareTo("Standard (Specular setup)") == 0 || m.shader.name.CompareTo("Standard") == 0)
                    m.shader = objShader;
                else if (m.shader.name.CompareTo("Artist/Scene/Terrain Bump Full") == 0)
                    m.shader = terrainShader;

                if (m.shader.name.CompareTo("Artist/Mobile Specular Standard") == 0)
                {
                    if (m.IsKeywordEnabled("_SPECGLOSSCOLOR"))
                    {
                        Color specColor = m.GetColor("_SpecColor");
                        if (Mathf.Approximately(specColor.r, 0.2f) && Mathf.Approximately(specColor.g, 0.2f) && Mathf.Approximately(specColor.b, 0.2f))
                        {
                            m.SetColor("_SpecColor", Color.clear);
                            m.DisableKeyword("_SPECGLOSSCOLOR");
                        }
                    }
                }
				m.renderQueue = queue;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void WriteTexture(Camera lightCam, Texture2D coordTex, Texture2D depthTex, MeshRenderer mr)
    {
        MeshFilter meshFilter = mr.GetComponent<MeshFilter>();
        Vector3[] vertices = meshFilter.sharedMesh.vertices;
        Vector2[] uvs = meshFilter.sharedMesh.uv2.Length == 0? meshFilter.sharedMesh.uv : meshFilter.sharedMesh.uv2;
        int[] triangles = meshFilter.sharedMesh.triangles;

        
        for (int triIndx = 0; triIndx < triangles.Length; triIndx += 3)
        {
            Vector2 uv0 = UV2LightMapUV(uvs[triangles[triIndx]], mr.lightmapScaleOffset);
            Vector2 uv1 = UV2LightMapUV(uvs[triangles[triIndx + 1]], mr.lightmapScaleOffset);
            Vector2 uv2 = UV2LightMapUV(uvs[triangles[triIndx + 2]], mr.lightmapScaleOffset);
            Vector3 v0 = vertices[triangles[triIndx]];
            Vector3 v1 = vertices[triangles[triIndx + 1]];
            Vector3 v2 = vertices[triangles[triIndx + 2]];

            double x0 = (double)uv0.x * (coordTex.width - 1);
            int y0 = (int)System.Math.Round((double)uv0.y * (coordTex.height - 1));
            double x1 = (double)uv1.x * (coordTex.width - 1);
            int y1 = (int)System.Math.Round((double)uv1.y * (coordTex.height - 1));
            double x2 = (double)uv2.x * (coordTex.width - 1);
            int y2 = (int)System.Math.Round((double)uv2.y * (coordTex.height - 1));
            
            int yMin = Mathf.Min(new int[] { y0, y1, y2 });
            int yMax = Mathf.Max(new int[] { y0, y1, y2 });
            for (int py = yMin; py <= yMax; ++py)
            {
                double xMin = coordTex.width - 1;
                double xMax = 0;
                if ((y0 - py) * (py - y1) >= 0)
                {
                    if (y0 == y1)
                    {
                        if (x0 > x1)
                        {
                            if (x0 > xMax)
                                xMax = x0;
                            if (x1 < xMin)
                                xMin = x1;
                        }
                        else
                        {
                            if (x1 > xMax)
                                xMax = x1;
                            if (x0 < xMin)
                                xMin = x0;
                        }
                    }
                    else
                    {
                        double dx = (double)(x0 - x1) / (y0 - y1);
                        double x = x1 + dx * (py - y1);
                        if (x < xMin)
                            xMin = x;
                        if (x > xMax)
                            xMax = x;
                    }
                }
                if ((y0 - py) * (py - y2) >= 0)
                {
                    if (y0 == y2)
                    {
                        if (x0 > x2)
                        {
                            if (x0 > xMax)
                                xMax = x0;
                            if (x2 < xMin)
                                xMin = x2;
                        }
                        else
                        {
                            if (x2 > xMax)
                                xMax = x2;
                            if (x0 < xMin)
                                xMin = x0;
                        }
                    }
                    else
                    {
                        double dx = (double)(x0 - x2) / (y0 - y2);
                        double x = x2 + dx * (py - y2);
                        if (x < xMin)
                            xMin = x;
                        if (x > xMax)
                            xMax = x;
                    }
                }
                if ((y1 - py) * (py - y2) >= 0)
                {
                    if (y1 == y2)
                    {
                        if (x1 > x2)
                        {
                            if (x1 > xMax)
                                xMax = x1;
                            if (x2 < xMin)
                                xMin = x2;
                        }
                        else
                        {
                            if (x2 > xMax)
                                xMax = x2;
                            if (x1 < xMin)
                                xMin = x1;
                        }
                    }
                    else
                    {
                        double dx = (double)(x1 - x2) / (y1 - y2);
                        double x = x2 + dx * (py - y2);
                        if (x < xMin)
                            xMin = x;
                        if (x > xMax)
                            xMax = x;
                    }
                }
                int iMinX = (int)System.Math.Ceiling(xMin);
                int iMaxX = (int)System.Math.Floor(xMax);
                for (int px = iMinX; px <= iMaxX; ++px)
                {
                    Vector2 pUV = new Vector2((float)px * coordTex.texelSize.x, (float)py * coordTex.texelSize.y);
                    Vector3 barycentric = CalcBarycentric(uv0, uv1, uv2, pUV);

                    Vector3 pixelLocalPosition = v0 * barycentric.x + v1 * barycentric.y + v2 * barycentric.z;
                    Vector3 pixelWorldPos = mr.transform.TransformPoint(pixelLocalPosition);
                    Vector3 pixelViewPos = lightCam.transform.InverseTransformPoint(pixelWorldPos);
                    pixelViewPos.x = (pixelViewPos.x / lightCam.orthographicSize + 1f) * 127.5f;
                    pixelViewPos.y = (pixelViewPos.y / lightCam.orthographicSize + 1f) * 127.5f;
                    pixelViewPos.z = (-pixelViewPos.z - lightCam.nearClipPlane) / (lightCam.farClipPlane - lightCam.nearClipPlane);
                    int xInt = Mathf.FloorToInt(pixelViewPos.x);
                    int yInt = Mathf.FloorToInt(pixelViewPos.y);
                    coordTex.SetPixel(px, py, new Color32((byte)xInt, (byte)Mathf.FloorToInt((pixelViewPos.x - xInt) * 255), (byte)yInt, (byte)Mathf.FloorToInt((pixelViewPos.y - yInt) * 255)));
                    depthTex.SetPixel(px, py, new Color(pixelViewPos.z, pixelViewPos.z, pixelViewPos.z));
                }
            }
        }
    }

    static Vector2 UV2LightMapUV(Vector2 uv, Vector4 scaleOffset)
    {
        return new Vector2(uv.x * scaleOffset.x + scaleOffset.z, uv.y * scaleOffset.y + scaleOffset.w);
    }

    static double HeronsFormula(double a, double b, double c)
    {
        double s = 0.5 * (a + b + c);
        return System.Math.Sqrt(s * (s - a) * (s - b) * (s - c));
    }

    static Vector3 CalcBarycentric(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 p)
    {
        Vector2 to0 = v0 - p;
        Vector2 to1 = v1 - p;
        Vector2 to2 = v2 - p;

        double a0 = System.Math.Sqrt((v2 - v1).sqrMagnitude);
        double a1 = System.Math.Sqrt((v2 - v0).sqrMagnitude);
        double a2 = System.Math.Sqrt((v0 - v1).sqrMagnitude);
        double c0 = System.Math.Sqrt(to0.sqrMagnitude);
        double c1 = System.Math.Sqrt(to1.sqrMagnitude);
        double c2 = System.Math.Sqrt(to2.sqrMagnitude);
        double s = HeronsFormula(a0, a1, a2);
        double s0 = HeronsFormula(a0, c1, c2);
        double s1 = HeronsFormula(a1, c0, c2);
        double s2 = HeronsFormula(a2, c1, c0);
        return new Vector3((float)(s0 / s), (float)(s1 / s), (float)(s2 / s));
    }
}
