using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using XMLEngineTools;
using System.IO;

public class AtlasCheck
{
    const string ATLAS_FOLDER = "/ResourcesLink/Prefabs/Atlas";
    private const string UI_FOLDER = "/Resources/Prefabs/UI";
    static List<string> _atlasFileList = new List<string>();
    private static List<string> _atlasUsedList = new List<string>();
    private static List<string> _atlasLog = new List<string>();
    static void GetAtlasFileList()
    {
        _atlasFileList = XMLEngineTools.BaseFun.GetFilesPathInDir(Application.dataPath + ATLAS_FOLDER, ".prefab");
    }

    static void PrintAtlas()
    {
        Debug.LogFormat("<color=white>------------------------  {0} --------------------------</color>",
            _atlasFileList.Count);
        foreach (var s in _atlasFileList)
        {
            Debug.LogFormat("<color=white>{0}</color>", s);
        }

        Debug.Log("<color=white>------------------------  --------------------------</color>");
    }

    static void GetAtlasFilesUsed()
    {
        _atlasUsedList.Clear();
        List<string> uiPrefab = XMLEngineTools.BaseFun.GetFilesPathInDir(Application.dataPath + UI_FOLDER, ".prefab");
        for (int i = 0; i < uiPrefab.Count; i++)
        {
            string[] depPaths = AssetDatabase.GetDependencies(uiPrefab[i]);
            for (int j = 0; j < depPaths.Length; j++)
            {
                if (depPaths[j].ToLower().Contains(".prefab") && depPaths[j].Contains("/Prefabs/Atlas/")
                                                              && !_atlasUsedList.Contains(depPaths[j]))
                {
                    _atlasUsedList.Add(depPaths[j]);
                }
            }
        }
    }

    static Dictionary<string, List<string>> AnalyzePrefabAtlas()
    {
        Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
        List<string> uiPrefab = XMLEngineTools.BaseFun.GetFilesPathInDir(Application.dataPath + UI_FOLDER, ".prefab");
        for (int i = 0; i < uiPrefab.Count; i++)
        {
            string[] depPaths = AssetDatabase.GetDependencies(uiPrefab[i]);
            for (int j = 0; j < depPaths.Length; j++)
            {
                if (depPaths[j].ToLower().Contains(".prefab") && depPaths[j].Contains("/Prefabs/Atlas/"))
                {
                    if (dict.ContainsKey(depPaths[j]))
                    {
                        dict[depPaths[j]].Add(uiPrefab[i]);
                    }
                    else
                    {
                        List<string> prefabList = new List<string>();
                        prefabList.Add(uiPrefab[i]);
                        dict.Add(depPaths[j], prefabList);
                    }
                }
            }
        }

        return dict;
    }

    static void PrintAtlasUsed()
    {
        Debug.LogFormat("<color=white>------------------------{0} --------------------------</color>",
            _atlasUsedList.Count);
        foreach (var s in _atlasUsedList)
        {
            Debug.LogFormat("<color=green>{0}</color>", s);
        }

        Debug.Log("<color=white>------------------------  --------------------------</color>");
    }

    [MenuItem]
    public static void CheckAtlas()
    {
        AtlasPrefabWin win = EditorWindow.GetWindow(typeof(AtlasPrefabWin)) as AtlasPrefabWin;
        win.position = new Rect(300, 300, 1000, 500);
        
    }

    public static void CheckAtlasInPrefab(string atlasName)
    {
        List<string> printedAtlas = new List<string>();
        Object[] selecteObjs = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel);
        for (int i = 0; i < selecteObjs.Length; i++)
        {
            GameObject go = selecteObjs[i] as GameObject;
            if (go != null)
            {
                UISprite[] sprites = go.GetComponentsInChildren<UISprite>(true);
                foreach (var s in sprites)
                {
                    if (s.atlas != null)
                    {
                        if (!string.IsNullOrEmpty(atlasName))
                        {
                            if (s.atlas.name.Contains(atlasName))
                                Debug.LogFormat("{0} - {1} use {2}", go.name, s.name, s.atlas.name);
                        }
                        else
                        {
                            if (!printedAtlas.Contains(s.atlas.name))
                            {
                                printedAtlas.Add(s.atlas.name);
                                Debug.LogFormat("{0} - use {1}", go.name, s.atlas.name);
                            }
                        }
                    }
                }
            }
        }
        Debug.LogFormat("atlas count is {0}", printedAtlas.Count);
    }
    [MenuItem]
    public static void PrintAtlasUsedByPrefab()
    {
        CheckAtlasInPrefab("");
    }

    [MenuItem]
    public static void PrintAllAtlasUsed()
    {
        _atlasLog.Clear();
        Dictionary<string, UsedAtlasInfo> all = new Dictionary<string, UsedAtlasInfo>();
        GetAllAtlasUsed("UI Root (2D)", ref all);
        GetAllAtlasUsed("UI Root (3D)", ref all);
        float maxSize = 0;
        foreach (KeyValuePair<string, UsedAtlasInfo> kv in all)
        {
            _atlasLog.Add(string.Format("", kv.Key, kv.Value.size.x, kv.Value.size.y, kv.Value.usedSprite.Count));
            _atlasLog.Add("-------------------------------");
            for (int i = 0; i < kv.Value.usedSprite.Count; i++)
            {
                string s1 = kv.Value.usedSprite[i].spriteName;
                _atlasLog.Add(string.Format(, s1, kv.Value.usedSprite[i].goNameList.Count));
                for (int j = 0; j < kv.Value.usedSprite[i].goNameList.Count; j++)
                {
                    _atlasLog.Add(kv.Value.usedSprite[i].goNameList[j]);
                }
                
            }
            _atlasLog.Add("-------------------------------");
            maxSize += kv.Value.size.x * kv.Value.size.y;
        }
        _atlasLog.Add(string.Format(":{1}", all.Count, maxSize));
        string path = Application.dataPath + "/EditorRes/";
        BaseFun.SaveToFile(path + "UsedAtlasInfo.txt", _atlasLog);
        EditorUtility.RevealInFinder(path + "UsedAtlasInfo.txt");
    }

    public class AtlasSpriteInfo
    {
        public string spriteName;
        public List<string> goNameList;
    }

    public class UsedAtlasInfo
    {
        public Vector2 size;
        public List<AtlasSpriteInfo> usedSprite;

        public AtlasSpriteInfo GetAtlasSpriteInfoByName(string s)
        {
            foreach (AtlasSpriteInfo info in usedSprite)
            {
                if (info.spriteName == s)
                {
                    return info;
                }
            }
            return null;
        }
    }

    public static string GetAbsoulutePath(Transform trans)
    {
        string s = trans.name;
        while(trans.parent)
        {
            trans = trans.parent;
            s = trans.name + "/" + s;
        }
        return s;
    }

    public static void GetAllAtlasUsed(string rootName, ref Dictionary<string, UsedAtlasInfo> dict)
    {
        GameObject go = GameObject.Find(rootName);
        if (go == null)
            return;

        UISprite[] sprites = go.GetComponentsInChildren<UISprite>(true);
        foreach (var s in sprites)
        {
            if (s.atlas != null)
            {
                if(!dict.ContainsKey(s.atlas.name))
                {
                    UsedAtlasInfo info = new UsedAtlasInfo();
                    info.size = new Vector2(s.atlas.texture.width, s.atlas.texture.height);
                    info.usedSprite = new List<AtlasSpriteInfo>();
                    AtlasSpriteInfo spriteInfo = new AtlasSpriteInfo();
                    spriteInfo.spriteName = s.spriteName;
                    spriteInfo.goNameList = new List<string>();
                    spriteInfo.goNameList.Add(GetAbsoulutePath(s.transform));
                    info.usedSprite.Add(spriteInfo);
                    dict.Add(s.atlas.name, info);
                }
                else
                {
                    AtlasSpriteInfo spriteInfo = dict[s.atlas.name].GetAtlasSpriteInfoByName(s.spriteName);
                    if (spriteInfo == null)
                    {
                        spriteInfo = new AtlasSpriteInfo();
                        spriteInfo.spriteName = s.spriteName;
                        spriteInfo.goNameList = new List<string>();
                        spriteInfo.goNameList.Add(GetAbsoulutePath(s.transform));
                        dict[s.atlas.name].usedSprite.Add(spriteInfo);
                    }
                    else
                    {
                        spriteInfo.goNameList.Add(GetAbsoulutePath(s.transform));
                    }
                }
            }
        }
    }
    
    #region CMD
    [MenuItem]
    public static void CMD_GetAtlasFileList()
    {

        GetAtlasFileList();
        PrintAtlas();

    }

    public static List<string> GetTexturePrefabLink(string atlasName, string textureName)
    {
        Dictionary<string, List<string>> dict = GetAlterPrefabDict();
        List<string> result = new List<string>();
        string atlasPath = "Assets/ResourcesLink/Prefabs/Atlas/" + atlasName + ".prefab";
        List<string> prefabList = null;
        if (dict.TryGetValue(atlasPath, out prefabList))
        {
            int count = 0;
            foreach (var s in prefabList)
            {
                GameObject obj = AssetDatabase.LoadAssetAtPath(s, typeof(GameObject)) as GameObject;
                if (obj)
                {
                    UISprite[] sprites = obj.GetComponentsInChildren<UISprite>(true);
                    foreach (var spr in sprites)
                    {
                        if (spr.spriteName == textureName)
                        {
                            string rs = s + " - " + GetTransfromPath(spr.transform);
                            result.Add(rs);
                            Debug.Log(rs);
                        }
                    }
                }
            }
        }
        return result;
    }

    public static string GetTransfromPath(Transform trans)
    {
        string s = trans.name;
        Transform p = trans.parent;
        while (p != null)
        {
            s = p.name + "/" + s;
            p = p.parent;
        }
        return s;
    }
    
     [MenuItem]
    public static void CMD_PrintPrefabWhichUseSelPic()
    {
        AtlasPrefabTextureWin win = EditorWindow.GetWindow(typeof(AtlasPrefabTextureWin)) as AtlasPrefabTextureWin;
        win.position = new Rect(300, 300, 1000, 500);
    }
    
    
    [MenuItem]
    public static void CMD_GetAtlasUsedList()
    {
        GetAtlasFilesUsed();
        PrintAtlasUsed();
    }

    public static Dictionary<string, List<string>> GetAlterPrefabDict()
    {
        string fileName = Application.dataPath + "/../Temp/PrefabAtlas.txt";
        List<string> lines = XMLEngineTools.BaseFun.ReadFileByLines(fileName);
        Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
        for (int i = 0; i < lines.Count; i++)
        {
            string[] infos = lines[i].Split(',');
            if (infos.Length == 0)
                continue;
            List<string> prfabList = new List<string>();
            for (int j = 1; j < infos.Length; j++)
            {
                prfabList.Add(infos[j]);
            }
            dict.Add(infos[0], prfabList);
        }

        return dict;
    }

    [MenuItem]
    public static void CMD_PrintLinkPrefab()
    {
















        Dictionary<string, List<string>> dict = GetAlterPrefabDict();
        
        Object[] objects = Selection.GetFiltered (typeof(Object), SelectionMode.TopLevel);
        for (int i = 0; i < objects.Length; i++)
        {
            string assetPath = AssetDatabase.GetAssetPath(objects[i]);
            List<string> prefabList = null;
            if (dict.TryGetValue(assetPath, out prefabList))
            {
                Debug.LogFormat("<color=green>--------------------------------------------------</color>", assetPath, prefabList.Count); 
                foreach (var s in prefabList)
                {
                    Debug.LogFormat("<color=green>{0}</color>", s);
                }
            }
            else
            {
                Debug.LogFormat("<color=green>--------------------------------------------------</color>", assetPath); 
            }
        }
        EditorUtility.DisplayDialog;
    }

    [MenuItem]
    public static void CMD_AnalyzePrefabAtlas()
    {
        Dictionary<string, List<string>> dict = AnalyzePrefabAtlas();
        string fileName = Application.dataPath + "/../Temp/PrefabAtlas.txt";
        FileStream fs = new FileStream(fileName, FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);
        foreach (KeyValuePair<string, List<string>> kv in dict)
        {
            string msg = kv.Key;
            foreach (var s in kv.Value)
            {
                msg = msg + "," + s;
            }
            sw.WriteLine(msg);
        }
        sw.Flush();
        sw.Close();
        sw.Dispose();
        fs.Close();
        fs.Dispose();   
        EditorUtility.DisplayDialog;
    }

    [MenuItem]
    public static void CMD_GetAtlasUnUsedList()
    {
        GetAtlasFileList();
        GetAtlasFilesUsed();
        List<string> unUsedList = new List<string>();
        foreach (var s in _atlasFileList)
        {
            if (!_atlasUsedList.Contains(s))
            {
                unUsedList.Add(s);
            }
        }
        Debug.LogFormat("<color=white>------------------------  --------------------------</color>", unUsedList.Count); 
        foreach (var s in unUsedList)
        {
            Debug.LogFormat("<color=red>{0}</color>", s);
        }
        Debug.Log("<color=white>------------------------ --------------------------</color>"); 
    }
    #endregion
}
