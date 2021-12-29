using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SpliteCharacterMaskTexture
{
    class TextureGroup
    {
        public Texture albedo;
        public Texture mask;
    }

    [MenuItem]
    static void Excute()
    {
        Dictionary<Texture2D, TextureGroup> textureMap = new Dictionary<Texture2D, TextureGroup>();
        Object[] materials = Selection.GetFiltered(typeof(Material), SelectionMode.DeepAssets);
        for (int i = 0; i < materials.Length; ++i)
        {
            Material mat = materials[i] as Material;
            if (!mat)
                continue;
            if (mat.name.Contains("head"))
                continue;
            if (!mat.shader.name.Equals("Artist/PlayerCharacter"))
            {
                Debug.Log("Material's shader is not Artist/PlayerCharacter", mat);
                continue;
            }
            if (mat.renderQueue != -1 && mat.renderQueue != 2000)
                continue;
            Texture2D texture = mat.GetTexture("_MainTex") as Texture2D;
            if (!texture)
                continue;

            TextureGroup group;
            if (!textureMap.TryGetValue(texture, out group))
            {
                string originTexPath = AssetDatabase.GetAssetPath(texture);
                int length = originTexPath.LastIndexOf('_');
                if (length < 0)
                    length = originTexPath.LastIndexOf('.');
                string maskPath = originTexPath.Substring(0, length) + "_mask.png";

                Texture2D maskAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                if (!maskAsset)
                {
                    TextureImporter ti = AssetImporter.GetAtPath(originTexPath) as TextureImporter;
                    ti.isReadable = true;
                    ti.textureFormat = TextureImporterFormat.AutomaticTruecolor;
                    ti.ClearPlatformTextureSettings(BuildTarget.Android.ToString());
                    ti.ClearPlatformTextureSettings(BuildTarget.iOS.ToString());
                    ti.SaveAndReimport();
                    AssetDatabase.Refresh();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(originTexPath);

                    Color[] colors = texture.GetPixels();
                    Texture2D mask = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
                    Color[] maskColors = new Color[colors.Length];
                    for (int p = 0; p < colors.Length; ++p)
                    {
                        maskColors[p] = new Color(0f, colors[p].a, 0f, 0f);
                    }
                    mask.SetPixels(maskColors);
                    mask.Apply();

                    byte[] bytes = mask.EncodeToPNG();
                    using (FileStream fs = new FileStream(maskPath, FileMode.Create))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                        fs.Flush();
                    }
                    AssetDatabase.Refresh();
                    ti = TextureImporter.GetAtPath(maskPath) as TextureImporter;
                    ti.textureType = TextureImporterType.Default;
                    ti.textureFormat = TextureImporterFormat.AutomaticCompressed;
                    ti.isReadable = false;
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                    AssetDatabase.Refresh();
                    maskAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

                    ti = AssetImporter.GetAtPath(originTexPath) as TextureImporter;
                    ti.textureType = TextureImporterType.Default;
                    ti.isReadable = false;
                    ti.textureFormat = TextureImporterFormat.AutomaticCompressed;
                    ti.SetPlatformTextureSettings(BuildTarget.Android.ToString(), 2048, TextureImporterFormat.ETC2_RGB4);
                    ti.SetPlatformTextureSettings(BuildTarget.iOS.ToString(), 2048, TextureImporterFormat.PVRTC_RGB4);
                    ti.mipmapEnabled = false;
                    ti.SaveAndReimport();
                    AssetDatabase.Refresh();
                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(originTexPath);
                }

                group = new TextureGroup() { albedo = texture, mask = maskAsset };
                textureMap.Add(texture, group);
            }

            mat.SetTexture("_MainTex", group.albedo);
            mat.SetTexture("_MaskTex", group.mask);
            mat.EnableKeyword("_USE_MASK");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
