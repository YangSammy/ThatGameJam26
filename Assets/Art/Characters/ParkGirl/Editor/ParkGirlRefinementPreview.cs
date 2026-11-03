using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace MemoryWhitebox.Art.Editor
{
    // Explicit isolated renderer for art inspection; no scene or asset saving.
    public static class ParkGirlRefinementPreview
    {
        const string Art="Assets/Art/Characters/ParkGirl";
        const string Output="output/Characters/ParkGirl/Preview/Unity/V2";

        [MenuItem("Tools/Art/Park Girl/Render Refinement Review")]
        public static void Render()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first.");
            var scene=EditorSceneManager.NewPreviewScene();
            RenderTexture rt=null;Texture2D image=null;Texture2D sheet=null;
            try
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/PF_ParkGirl.prefab"),scene);
                var visual=root.transform.Find("Visual").gameObject;
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Art+"/Animations/AN_ParkGirl_Sample_10s.anim");
                var cameraObject=new GameObject("ArtReviewCamera");SceneManager.MoveGameObjectToScene(cameraObject,scene);
                var cam=cameraObject.AddComponent<Camera>();cam.scene=scene;cam.enabled=false;cam.orthographic=true;cam.nearClipPlane=.02f;cam.farClipPlane=15;
                cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.115f,.132f,.145f);
                foreach(var pair in new[]{new Vector2(-35,.85f),new Vector2(140,.26f)})
                {
                    var lightObject=new GameObject("ArtReviewLight");SceneManager.MoveGameObjectToScene(lightObject,scene);
                    var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=pair.y;light.transform.rotation=Quaternion.Euler(38,pair.x,0);
                }
                Directory.CreateDirectory(Output);
                int width=720,height=900;
                rt=new RenderTexture(width,height,24);rt.antiAliasing=4;rt.Create();cam.targetTexture=rt;
                image=new Texture2D(width,height,TextureFormat.RGB24,false);sheet=new Texture2D(width*3,height,TextureFormat.RGB24,false);
                var cameras=new[]{new Vector3(2.2f,1.45f,3.4f),new Vector3(-.65f,1.48f,1.8f),new Vector3(.8f,1.50f,-1.8f)};
                for(int pose=0;pose<2;pose++)
                {
                    clip.SampleAnimation(visual,pose==0?0:6);
                    var skins=visual.GetComponentsInChildren<SkinnedMeshRenderer>();var temporary=new List<GameObject>();var meshes=new List<Mesh>();
                    foreach(var skin in skins)
                    {
                        var mesh=new Mesh();skin.BakeMesh(mesh,true);meshes.Add(mesh);
                        var go=new GameObject("EvaluatedReviewMesh");SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetParent(skin.transform,false);
                        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;temporary.Add(go);
                    }
                    for(int view=0;view<3;view++)
                    {
                        cam.orthographicSize=view==0?.91f:.36f;cam.transform.position=cameras[view];cam.transform.LookAt(new Vector3(0,view==0?.82f:1.32f,0));cam.Render();
                        var old=RenderTexture.active;try{RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();}finally{RenderTexture.active=old;}
                        File.WriteAllBytes(Output+"/pose_"+pose+"_view_"+view+".png",image.EncodeToPNG());sheet.SetPixels(view*width,0,width,height,image.GetPixels());
                    }
                    sheet.Apply();File.WriteAllBytes(Output+"/review_pose_"+pose+".png",sheet.EncodeToPNG());
                    foreach(var go in temporary)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;
                }
            }
            finally
            {
                if(rt!=null){rt.Release();UnityEngine.Object.DestroyImmediate(rt);}if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(sheet!=null)UnityEngine.Object.DestroyImmediate(sheet);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
