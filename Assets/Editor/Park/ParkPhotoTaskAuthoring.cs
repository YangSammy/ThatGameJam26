using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace MemoryWhitebox.Editor
{
    public static class ParkPhotoTaskAuthoring
    {
        static Transform Child(Transform parent,string name,Vector3 position)
        {var t=parent.Find(name);if(t==null){t=new GameObject(name).transform;t.SetParent(parent,true);}t.position=position;return t;}
        public static string Apply()
        {
            if(Application.isPlaying)throw new Exception("Edit mode required");
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/ParkScene.unity");
            if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/ParkScene.unity",OpenSceneMode.Additive);
            if(scene.isDirty)throw new Exception("Save or discard ParkScene changes before authoring photography references.");
            Directory.CreateDirectory("Backups/ParkPhotoTasks-2026-10-31");
            if(!File.Exists("Backups/ParkPhotoTasks-2026-10-31/ParkScene.before.unity"))EditorSceneManager.SaveScene(scene,"Backups/ParkPhotoTasks-2026-10-31/ParkScene.before.unity",true);
            SceneManager.SetActiveScene(scene);
            var p=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ParkPhotoPrototype>(true)).Single();var c=p.companion;
            var anchors=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Anchors");if(anchors==null){anchors=new GameObject("Anchors");SceneManager.MoveGameObjectToScene(anchors,scene);}
            var group=Child(anchors.transform,"Photography Tasks",Vector3.zero);
            int[] indices={0,7,12};string[] names={"Entrance","Bridge","Meadow"};
            Vector3[] eyes={new Vector3(-13.5f,1.74f,-10),new Vector3(2.7f,2.2f,0),new Vector3(12.7f,1.74f,7.3f)};
            for(int i=0;i<3;i++)
            {
                var w=c.route[indices[i]];w.photoTask=(ParkPhotoWaypoint.PhotoTask)(i+1);var root=Child(group,names[i],w.transform.position);
                if(w.lookAt==null)w.lookAt=Child(root,"Reference Look Target",w.transform.position+(i==0?new Vector3(2,1,2):new Vector3(0,1,3)));
                w.referenceView=Child(root,"Reference Camera",eyes[i]);w.referenceView.rotation=Quaternion.LookRotation(w.transform.position+Vector3.up*1.25f-eyes[i]);w.referenceEv100=13;
                if(i==2){var toward=eyes[i]-w.transform.position;toward.y=0;toward.Normalize();var right=Quaternion.LookRotation(toward)*Vector3.right;w.handContact=Child(root,"Hand Contact",w.transform.position+toward*.40f+right*.25f+Vector3.up*1.08f);}
                EditorUtility.SetDirty(w);
            }
            var headPivot=Child(c.visual,"Photo Head Pivot",c.head.position);headPivot.rotation=c.visual.rotation;
            var hairPivot=Child(headPivot,"Photo Hair Pivot",c.head.position);hairPivot.rotation=headPivot.rotation;
            foreach(var t in c.visual.GetComponentsInChildren<Transform>(true).ToArray())
            {
                if(t==headPivot||t==hairPivot)continue;
                if(t.name=="Head"||t.name=="Nose"||t.name=="Eye"||t==c.head)t.SetParent(headPivot,true);
                else if(t.name=="Hair"||t.name=="Placeholder_LongHair_Replaceable")t.SetParent(hairPivot,true);
            }
            c.photoHeadPivot=headPivot;c.photoHairPivot=hairPivot;c.photoRightHand=c.rightArm.Find("Hand");EditorUtility.SetDirty(c);
            const string folder="Assets/Art/Photography/References";Directory.CreateDirectory(folder);
            var cp=c.transform.position;var cr=c.transform.rotation;var vp=p.view.transform.position;var vr=p.view.transform.rotation;float vf=p.view.fieldOfView;int mask=p.view.cullingMask;var previous=p.view.targetTexture;var active=RenderTexture.active;
            try
            {
                for(int i=0;i<3;i++)
                {
                    var w=c.route[indices[i]];c.transform.position=w.transform.position;var dir=w.lookAt.position-w.transform.position;dir.y=0;c.transform.rotation=Quaternion.LookRotation(dir);
                    p.view.transform.SetPositionAndRotation(w.referenceView.position,w.referenceView.rotation);p.view.fieldOfView=PlayerControlSettings.Current.parkAimingFov;p.view.cullingMask&=~(1<<FirstPersonHands.HandLayer);
                    var rt=RenderTexture.GetTemporary(960,600,24);var image=new Texture2D(960,600,TextureFormat.RGB24,false);
                    try{p.view.targetTexture=rt;p.view.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,960,600),0,0);image.Apply();File.WriteAllBytes(folder+"/"+names[i]+"Reference.png",image.EncodeToPNG());}
                    finally{p.view.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
                    string path=folder+"/"+names[i]+"Reference.png";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.SaveAndReimport();w.referencePhoto=AssetDatabase.LoadAssetAtPath<Texture2D>(path);EditorUtility.SetDirty(w);
                }
            }
            finally{c.transform.SetPositionAndRotation(cp,cr);p.view.transform.SetPositionAndRotation(vp,vr);p.view.fieldOfView=vf;p.view.cullingMask=mask;p.view.targetTexture=previous;RenderTexture.active=active;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();return p.CheckPhotographyRoute();
        }
    }
}
