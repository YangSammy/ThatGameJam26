using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class WardRuntimeAuthoring
    {
        const string Art="Assets/Art/Environments/WardPreview/";
        public static void Author()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"WardRuntimeBlankPaper.png");
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=true;importer.npotScale=TextureImporterNPOTScale.None;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var template=AssetDatabase.LoadAssetAtPath<Material>(Art+"WardSoftReveal.mat");
            const string path="Assets/Resources/WardRuntimeReveal.mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path);
            var mat=new Material(template){name="WardRuntimeReveal"};
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"WardRuntimeBlankPaper.png");
            mat.mainTexture=texture;
            mat.SetFloat("_ImageAspect",(float)texture.width/texture.height);
            mat.SetFloat("_UsePageInk",1);mat.SetFloat("_SceneBlend",1);
            mat.SetFloat("_RevealStyle",1);mat.SetFloat("_MeltSoftness",.060f);
            mat.SetFloat("_RevealTop",.78f);
            if(existing==null)AssetDatabase.CreateAsset(mat,path);
            else {EditorUtility.CopySerialized(mat,existing);Object.DestroyImmediate(mat);EditorUtility.SetDirty(existing);}
            AssetDatabase.SaveAssets();
            Debug.Log("WARD_RUNTIME_MATERIAL_READY");
        }
    }
}
