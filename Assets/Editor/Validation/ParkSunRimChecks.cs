using System;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    public static class ParkSunRimChecks
    {
        public static string Run()
        {
            var root=new GameObject("Temporary sun rim validation"){hideFlags=HideFlags.HideAndDontSave};
            root.transform.position=new Vector3(10000,10000,10000);
            int checks=0;Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);checks++;};
            try
            {
                var subject=new GameObject("Subject");subject.transform.SetParent(root.transform,false);
                var viewer=new GameObject("Viewer");viewer.transform.SetParent(root.transform,false);viewer.transform.localPosition=Vector3.back*3;
                var sunGo=new GameObject("Sun");sunGo.transform.SetParent(root.transform,false);
                var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.shadows=LightShadows.Hard;sunGo.transform.rotation=Quaternion.Euler(90,0,0);
                var rimGo=new GameObject("Rim");rimGo.transform.SetParent(root.transform,false);
                var rim=rimGo.AddComponent<ParkCharacterRimLight>();rim.subject=subject.transform;rim.viewer=viewer.transform;rim.sun=sun;
                rim.UpdatePose();check(rim.SampleSunVisibility()==1&&rimGo.GetComponent<Light>().intensity==60,"Open sunlight");
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(root.transform,false);blocker.transform.localPosition=Vector3.up*4;blocker.transform.localScale=new Vector3(4,1,4);
                Physics.SyncTransforms();rim.UpdatePose();check(rim.SampleSunVisibility()==0&&rimGo.GetComponent<Light>().intensity==3,"Opaque cover gives weak rim");
                blocker.GetComponent<Collider>().isTrigger=true;Physics.SyncTransforms();check(rim.SampleSunVisibility()==1,"Triggers ignored");
                UnityEngine.Object.DestroyImmediate(blocker.GetComponent<Collider>());
                blocker.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                rim.shadowMeshes=new[]{blocker.GetComponent<MeshFilter>()};
                blocker.transform.localRotation=Quaternion.Euler(10,20,25);
                check(rim.SampleSunVisibility()==0,"Rotated nonuniform shadow mesh without collider");
                blocker.SetActive(false);check(rim.SampleSunVisibility()==1,"Inactive shadow mesh ignored");
                blocker.SetActive(true);blocker.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                check(rim.SampleSunVisibility()==1,"Non-shadow renderer ignored");
                var self=subject.AddComponent<BoxCollider>();self.center=Vector3.up*2;self.size=new Vector3(1,1,1);Physics.SyncTransforms();
                check(rim.SampleSunVisibility()==1,"Subject self collision ignored");
                sun.enabled=false;rim.UpdatePose();check(rim.SampleSunVisibility()==0&&rimGo.GetComponent<Light>().intensity==3,"Disabled sun weak rim");
                sun.enabled=true;rim.sun=null;rim.UpdatePose();check(rimGo.GetComponent<Light>().intensity==3,"Missing sun safe fallback");
                rim.sun=sun;subject.SetActive(false);rim.UpdatePose();check(!rimGo.GetComponent<Light>().enabled,"Inactive subject disables light");
                return checks+"/"+checks+" sun/shade checks passed";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);Physics.SyncTransforms();}
        }
    }
}
