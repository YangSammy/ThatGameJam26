using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

namespace MemoryWhitebox.Art.Editor
{
    [InitializeOnLoad]
    public static class ParkGirlPlayValidation
    {
        const string Key="ParkGirl.SampleValidation";
        const string Out="output/Characters/ParkGirl";
        static readonly List<object> samples=new List<object>();
        static float start=-1,last=-1;
        static int capture;
        static ParkGirlPlayValidation()
        {
            EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged+=Changed;
        }
        public static void Start()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Already in Play Mode.");
            var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            if(scenes.Any(s=>s.isDirty))throw new InvalidOperationException("An open scene is dirty; validation will not close it.");
            SessionState.SetString(Key+".restore",JsonConvert.SerializeObject(scenes.Select(s=>s.path).ToArray()));
            SessionState.SetBool(Key,true);
            EditorSceneManager.OpenScene(ParkGirlSampleBuild.ScenePath,OpenSceneMode.Single);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(SessionState.GetBool(Key+".restorePending",false)&&!EditorApplication.isPlayingOrWillChangePlaymode&&EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+".restoreAfter",0))
            {
                SessionState.SetBool(Key+".restorePending",false);
                var restore=JsonConvert.DeserializeObject<string[]>(SessionState.GetString(Key+".restore","[]"));
                for(int i=0;i<restore.Length;i++)if(!string.IsNullOrEmpty(restore[i]))EditorSceneManager.OpenScene(restore[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
            }
            if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isPaused)return;
            if(SceneManager.GetActiveScene().path!=ParkGirlSampleBuild.ScenePath)return;
            if(start<0){start=Time.realtimeSinceStartup;samples.Clear();capture=0;}
            float elapsed=Time.realtimeSinceStartup-start;
            var go=GameObject.Find("ParkGirl_Sample");if(go==null)return;
            var animator=go.GetComponentInChildren<Animator>();if(animator==null)return;
            if(elapsed-last>=.1f)
            {
                last=elapsed;var hand=go.GetComponentsInChildren<Transform>().First(t=>t.name=="HandContactAnchor");
                var skin=go.GetComponentsInChildren<SkinnedMeshRenderer>().First(t=>t.name=="Body");
                int shape=Enumerable.Range(0,skin.sharedMesh.blendShapeCount).First(i=>skin.sharedMesh.GetBlendShapeName(i).Contains("Elbow1.R"));
                samples.Add(new {time=elapsed,normalizedTime=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                    root=new[]{go.transform.position.x,go.transform.position.y,go.transform.position.z},
                    hand=new[]{hand.position.x,hand.position.y,hand.position.z},elbow=skin.GetBlendShapeWeight(shape)});
            }
            if(capture<4 && elapsed>=new[]{.3f,3f,6f,9f}[capture])
            {
                var cam=Camera.main;var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
                var rt=new RenderTexture(900,1100,24);var tex=new Texture2D(900,1100,TextureFormat.RGB24,false);
                try{rt.Create();cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,1100),0,0);tex.Apply();File.WriteAllBytes(Out+"/Preview/Unity/play_"+capture+".png",tex.EncodeToPNG());}
                finally{cam.targetTexture=oldTarget;RenderTexture.active=oldActive;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
                capture++;
            }
            if(elapsed>=12f)
            {
                File.WriteAllText(Out+"/Reports/unity-play-samples.json",JsonConvert.SerializeObject(new {scene=SceneManager.GetActiveScene().path,seconds=elapsed,captures=capture,samples},Formatting.Indented));
                EditorApplication.isPlaying=false;
            }
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            SessionState.SetBool(Key+".restorePending",true);
            SessionState.SetFloat(Key+".restoreAfter",(float)EditorApplication.timeSinceStartup+1f);
            start=-1;last=-1;
        }
    }
}
