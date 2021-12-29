using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

public class CustomEditorFunction
{
    [MenuItem("ResetShaderName")]
    public static void ResetMaterials()
    {
        Dictionary<string, string> shaderReplaceMap = new Dictionary<string, string>();
        shaderReplaceMap.Add("FXMaker/Mask Additive Tint", "Artist/Mask Additive Tint");
        shaderReplaceMap.Add("FXMaker/Mask Additive Tint + 1", "Artist/Mask Additive Tint +1");
        shaderReplaceMap.Add("FXMaker/Mask Alpha Blended Tint", "Artist/Mask Alpha Blended Tint");
        shaderReplaceMap.Add("FXMaker/Mask Alpha Blended Tint + 1", "Artist/Mask Alpha Blended Tint +1");
        shaderReplaceMap.Add("Custom/Unlit/Transparent", "Artist/Semi Transparent Texture");
        shaderReplaceMap.Add("Custom/Mobile/Diffuse", "Artist/Diffuse");
        shaderReplaceMap.Add("Diffuse", "Artist/Diffuse");
        shaderReplaceMap.Add("Custom/Mobile/Particles/Additive Culled", "Artist/Additive Tint");
        shaderReplaceMap.Add("Mobile/Particles/Additive Culled", "Artist/Additive Tint");
        shaderReplaceMap.Add("Custom/Mobile/Particles/Additive Culled + 1", "Artist/Additive Tint +1");
        shaderReplaceMap.Add("Custom/Mobile/Particles/Alpha Blended", "Artist/Alpha Blended Tint");
        shaderReplaceMap.Add("Mobile/Particles/Alpha Blended", "Artist/Alpha Blended Tint");

        shaderReplaceMap.Add("ZombieStyle/MobileRimDiffuseCutoutAlpha", "Artist/Rim2 Diffuse");
        shaderReplaceMap.Add("MU/OverlayTransparent", "Artist/Rim Diffuse Overlay +1");

        Dictionary<Shader, List<string>> shaderUsageMap = new Dictionary<Shader, List<string>>();

        Object[] objects = Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets);
        for (int i = 0; i < objects.Length; ++i)
        {
            Material mat = objects[i] as Material;
            if (!mat)
                continue;
            string replaceShader = null;
            if (shaderReplaceMap.TryGetValue(mat.shader.name, out replaceShader))
            {
                RPDebug.Log(mat.shader.name, mat);
                mat.shader = Shader.Find(replaceShader);
            }
            else
            {
                List<string> assetList = null;
                if (!shaderUsageMap.TryGetValue(mat.shader, out assetList))
                {
                    assetList = new List<string>();
                    shaderUsageMap.Add(mat.shader, assetList);
                }
                assetList.Add(AssetDatabase.GetAssetPath(mat));
            }
        }
        RPDebug.Log("Reset Shader Finished");
        Dictionary<Shader, List<string>>.Enumerator itr = shaderUsageMap.GetEnumerator();
        while (itr.MoveNext())
        {
            StringBuilder usageLogBuilder = new StringBuilder();
            usageLogBuilder.Append(itr.Current.Key.name).Append(" : ").AppendLine(AssetDatabase.GetAssetPath(itr.Current.Key));
            List<string> assetList = itr.Current.Value;
            for (int i = 0; i < assetList.Count; ++i)
            {
                usageLogBuilder.Append('\t').AppendLine(assetList[i]);
            }
            RPDebug.Log(usageLogBuilder.ToString());
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("SDK/Custom/Check UI Material Usage")]
    public static void CheckUIAssetsUsage()
    {
        Dictionary<Material, List<GameObject>> materialUsageMap = new Dictionary<Material, List<GameObject>>();

        Object[] objects = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        for (int i = 0; i < objects.Length; ++i)
        {
            GameObject prefab = objects[i] as GameObject;
            if (!prefab)
                continue;
            GameObject instance = GameObject.Instantiate(prefab) as GameObject;
            UIWidget[] widgets = instance.GetComponentsInChildren<UIWidget>();
            Dictionary<int, Material> usedMaterials = new Dictionary<int, Material>();
            for (int j = 0; j < widgets.Length; ++j)
            {
                if (!widgets[j].material)
                    continue;
                Material m = widgets[j].material;
                if (!usedMaterials.ContainsKey(m.GetInstanceID()))
                    usedMaterials.Add(m.GetInstanceID(), m);
            }
            GameObject.DestroyImmediate(instance);

            Dictionary<int, Material>.Enumerator itr = usedMaterials.GetEnumerator();
            while (itr.MoveNext())
            {
                List<GameObject> objList = null;
                if (!materialUsageMap.TryGetValue(itr.Current.Value, out objList))
                {
                    objList = new List<GameObject>();
                    materialUsageMap.Add(itr.Current.Value, objList);
                }
                objList.Add(prefab);
            }
        }

        Dictionary<Material, List<GameObject>>.Enumerator matItr = materialUsageMap.GetEnumerator();
        while (matItr.MoveNext())
        {
            StringBuilder logBuilder = new StringBuilder();
            logBuilder.AppendLine(AssetDatabase.GetAssetPath(matItr.Current.Key));
            if (!matItr.Current.Key)
                continue;

            Shader shader = Shader.Find(matItr.Current.Key.shader.name);
            if (shader)
            {
                logBuilder.Append(" | ").Append(AssetDatabase.GetAssetPath(shader));
            }

            List<GameObject> objList = matItr.Current.Value;
            for (int i = 0; i < objList.Count; ++i)
            {
                logBuilder.Append('\t').AppendLine(AssetDatabase.GetAssetPath(objList[i]));
            }
            RPDebug.Log(logBuilder.ToString(), matItr.Current.Key);
        }

        RPDebug.Log("Finish Check Material Usages");
    }

    [MenuItem("SDK/Custom/Check Monobehaviour Usage")]
    public static void CheckScriptUsage()
    {
        HashSet<string> usedTypes = new HashSet<string>();
        HashSet<string> scripts = new HashSet<string>();

        string[] files = Directory.GetFiles(Application.dataPath + "/Scripts/XMLEngine/Common", "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; ++i)
        {
            int start = files[i].LastIndexOf('\\') + 1;
            int end = files[i].LastIndexOf('.');
            string scriptName = files[i].Substring(start, end - start);
            scripts.Add(scriptName);
        }

        Object[] objects = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        for (int i = 0; i < objects.Length; ++i)
        {
            GameObject prefab = objects[i] as GameObject;
            GameObject instance = GameObject.Instantiate(prefab) as GameObject;
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>();
            for (int j = 0; j < behaviours.Length; ++j)
            {
                if (!behaviours[j])
                    continue;
                usedTypes.Add(behaviours[j].GetType().ToString());
            }
            GameObject.DestroyImmediate(instance);
        }

        List<string> unusedScripts = new List<string>();
        HashSet<string>.Enumerator itr = scripts.GetEnumerator();
        while (itr.MoveNext())
        {
            if (!usedTypes.Contains(itr.Current) && !itr.Current.Contains("Loader"))
            {
                unusedScripts.Add(itr.Current.ToString());
            }
        }

        for (int i = 0; i < unusedScripts.Count; ++i)
        {
            RPDebug.Log(unusedScripts[i]);
        }
    }

    [MenuItem("SDK/Custom/Export Selected Bundle")]
    public static void ExportBundle()
    {
        Object[] objs = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
        if (objs == null || objs.Length == 0)
            return;

        AssetBundleBuild[] builds = new AssetBundleBuild[objs.Length];
        for (int i = 0; i < builds.Length; ++i)
        {
            string path = AssetDatabase.GetAssetPath(objs[i]);
            builds[i].assetBundleName = objs[i].name + ".unity3d";
            builds[i].assetNames = new string[1] { path };
        }
        BuildPipeline.BuildAssetBundles(Application.dataPath + "/../Assetbundles", builds, BuildAssetBundleOptions.None, BuildTarget.Android);
    }

    
    
    
    
    
    
    
    
    

    
    
    
    
    
    
    
    
    

    
    
    
    

    
    
    
    
    
    
               
    
    
    
    
}
