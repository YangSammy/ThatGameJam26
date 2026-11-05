using System;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ParkPhotoVisibilityChecks
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        public static string Run()
        {
            var type=typeof(ParkPhotoPrototype);var debounce=type.GetMethod("DebounceObstruction",BindingFlags.Static|BindingFlags.NonPublic);
            object[] state={false,.1f,0f,0f,false};
            bool cue=false;for(int i=0;i<3;i++){state[4]=cue;cue=(bool)debounce.Invoke(null,state);}
            if(!cue)throw new Exception("Sustained blocker has no feedback");
            state[0]=true;state[4]=cue;cue=(bool)debounce.Invoke(null,state);if(!cue)throw new Exception("Clear feedback flickers before release delay");
            state[1]=.05f;state[4]=cue;cue=(bool)debounce.Invoke(null,state);if(cue)throw new Exception("Clear feedback does not recover");
            var root=new GameObject("QA subject visibility");root.SetActive(false);
            try
            {
                var p=root.AddComponent<ParkPhotoPrototype>();p.enabled=false;
                var cam=new GameObject("QA camera").AddComponent<Camera>();cam.transform.SetParent(root.transform);cam.transform.position=new Vector3(1000,2,0);p.view=cam;
                var girl=new GameObject("QA girl");girl.transform.SetParent(root.transform);p.companion=girl.AddComponent<ParkCompanion>();p.companion.enabled=false;
                p.companion.chest=new GameObject("QA chest").transform;p.companion.chest.SetParent(girl.transform);p.companion.chest.position=new Vector3(1000,2,4);
                var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.transform.SetParent(root.transform);blocker.transform.position=new Vector3(1000,2,2);
                root.SetActive(true);var method=type.GetMethod("SubjectVisible",Hidden);
                blocker.transform.localScale=new Vector3(.06f,.06f,.06f);Physics.SyncTransforms();
                if(!(bool)method.Invoke(p,null))throw new Exception("One clear shoulder should permit visibility");
                blocker.transform.localScale=Vector3.one;Physics.SyncTransforms();
                if((bool)method.Invoke(p,null))throw new Exception("Opaque blocker should block chest and shoulders");
                blocker.layer=FirstPersonHands.HandLayer;Physics.SyncTransforms();
                if(!(bool)method.Invoke(p,null))throw new Exception("Player instruments must not occlude subject ray");
                return "PASS visibility: debounced block/clear, partial shoulders, full blocker, player-hand layer ignored";
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
