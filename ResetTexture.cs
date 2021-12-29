using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ResetTexture : ScriptableObject
{
    static private List<string> cacheDdsFile = new List<string>();
    static private List<string> delFailedFile = new List<string>();

    [MenuItem("DDS/DDS to TGA Replace")]
    static void ResetTextureToTGA()
    {
        ResetTex(0);
    }

    [MenuItem("DDS/DDS to PSD Replace")]
    static void ResetTextureToPSD()
    {
        ResetTex(1);
    }

    [MenuItem("DDS/DDS to TIFF Replace")]
    static void ResetTextureToTIFF()
    {
        ResetTex(2);
    }

    static void ResetTex(int type)
    {
        Object[] objs = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        GameObject gameObj = null;
        foreach (Object obj in objs)
        {
            RPDebug.Log("Obj name: " + obj.name);

            gameObj = obj as GameObject;
            if (gameObj != null) 
            {
                RecursionChild(gameObj, type);
            }
        }
        if (cacheDdsFile.Count > 0)
        {
            if (EditorUtility.DisplayDialog)
            {
                foreach (string ddsFileName in cacheDdsFile)
                {
                    if (!AssetDatabase.MoveAssetToTrash(ddsFileName))
                    {
                        delFailedFile.Add(ddsFileName);
                    }
                }

                if (delFailedFile.Count > 0)
                {
                    string filenames = "";
                    foreach (string strName in delFailedFile)
                    {
                        filenames += strName + ";\r\n";
                    }
                    EditorUtility.DisplayDialog( filenames);
                }
                else
                {
                    EditorUtility.DisplayDialog;
                }

            }
        }

        cacheDdsFile.Clear();
        delFailedFile.Clear();
    }

    
    static void RecursionChild(GameObject gameObj, int type)
    {
        
        if (gameObj.GetComponent<Renderer>() != null)
        {
            ResetTextureByType(gameObj.GetComponent<Renderer>().sharedMaterials, type);
        }

        foreach (Transform tran in gameObj.transform)
        {
            RecursionChild(tran.gameObject, type);
        }
    }

    static void ReplaceTexture(Material mat, int type, string texName = "")
    {
        Texture2D tex = null;
        Texture tempTex = null;
        if (texName.Length == 0)
        {
            tex = mat.mainTexture as Texture2D;
        }
        else
        {
            tex = mat.GetTexture(texName) as Texture2D;
        }

        string texPath = AssetDatabase.GetAssetPath(tex);
        if (texPath != null && texPath.Length > 0 && texPath.EndsWith(".dds"))
        {
            switch (type)
            {
                case 0:
                    texPath = texPath.Replace(".dds", ".tga");
                    break;
                case 1:
                    texPath = texPath.Replace(".dds", ".psd");
                    break;
                case 2:
                    texPath = texPath.Replace(".dds", ".tiff");
                    break;
                default:
                    texPath = texPath.Replace(".dds", ".tga");
                    break;
            }

            tempTex = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture)) as Texture;
            if (tempTex != null)
            {
                if (texName.Length == 0)
                {
                    mat.mainTexture = tempTex;
                }
                else
                {
                    mat.SetTexture(texName, tempTex);
                }

                if (!cacheDdsFile.Contains(texPath))
                {
                    cacheDdsFile.Add(texPath);
                }
            }
        }
    }

    
    static void ResetTextureByType(Material[] mats, int type)
    {
        foreach (Material mat in mats)
        {
            Texture2D tex = mat.mainTexture as Texture2D;
            if (tex != null)
            {
                ReplaceTexture(mat, type);
            }

            if (mat.HasProperty("_Mask"))
            {
                tex = mat.GetTexture("_Mask") as Texture2D;
                if (tex != null)
                {
                    ReplaceTexture(mat, type, "_Mask");
                }
            }
        }
    }
}
