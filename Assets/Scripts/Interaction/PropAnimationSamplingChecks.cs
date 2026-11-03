using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MemoryWhitebox.Art;
namespace MemoryWhitebox
{
    // Opt-in player regression: launch a development player with -qa-prop-sampling.
    // Clones visual assets only; never enters chapters or reads/writes progress.
    public sealed class PropAnimationSamplingChecks : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (!Environment.GetCommandLineArgs().Contains("-qa-prop-sampling")) return;
            new GameObject("Prop sampling validation").AddComponent<PropAnimationSamplingChecks>();
        }
        IEnumerator Start()
        {
            yield return null; // House scene builds its darkroom presentation during startup.
            try { Debug.Log(Run()); }
            catch (Exception e) { Debug.LogError("PROP_SAMPLING_FAIL " + e); }
            finally { Destroy(gameObject); }
        }
#endif
        static float[] Pose(GameObject root)
        {
            var result = new List<float>();
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                var p=t.localPosition; var q=t.localRotation; var s=t.localScale;
                result.AddRange(new[]{p.x,p.y,p.z,q.x,q.y,q.z,q.w,s.x,s.y,s.z,t.gameObject.activeSelf?1f:0f});
            }
            foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                for (int i=0;i<skin.sharedMesh.blendShapeCount;i++) result.Add(skin.GetBlendShapeWeight(i));
            return result.ToArray();
        }
        static bool Same(float[] a,float[] b) => a.Length==b.Length && a.Zip(b,(x,y)=>Mathf.Abs(x-y)<.00001f).All(x=>x);
        static void Check(GameObject root,Action rest,Action changed,string label)
        {
            rest(); var before=Pose(root); changed(); var after=Pose(root);
            if(Same(before,after))throw new Exception(label+": sampled pose did not change");
            rest(); if(!Same(before,Pose(root)))throw new Exception(label+": rest pose did not restore");
            changed(); if(!Same(after,Pose(root)))throw new Exception(label+": repeated pose is not deterministic");
            var animator=root.GetComponent<Animator>();
            if(animator==null || animator.runtimeAnimatorController!=null || animator.enabled || animator.applyRootMotion)
                throw new Exception(label+": expected a disabled, controller-free sampling Animator");
        }
        static void WithClone(GameObject source,Action<GameObject> action)
        {
            if(source==null)throw new Exception("Missing validation asset");
            var clone=Instantiate(source);clone.name="QA sampling clone";clone.transform.position=Vector3.one*10000;
            try{action(clone);}finally{clone.SetActive(false);Destroy(clone);}
        }
        public static string Run()
        {
            if(!Application.isPlaying)throw new Exception("Run in Play or the standalone player");
            var photo=PhotographyVisualAssets.Load();int count=0;
            foreach(var film in photo.films)WithClone(film,go=>{
                Check(go,()=>PhotographyCameraVisual.SampleFilm(photo,go,0,0),()=>PhotographyCameraVisual.SampleFilm(photo,go,1,.65f),"B film");count++;
            });
            WithClone(WritingVisualAssets.Load().writingSet,go=>{
                var visual=go.GetComponent<WritingPropVisual>();
                Check(go,visual.ResetVisual,()=>visual.SetCrumpleProgress(.65f),"D paper");count++;
            });
            WithClone(PaintingVisualAssets.Load().door,go=>{
                var visual=go.GetComponent<AlbumWinterVisual>();
                Check(go,visual.ResetVisual,()=>visual.SetDoorState(1,1,.65f,true),"E door");count++;
            });
            var darkroom=FindObjectOfType<DarkroomAssetVisuals>(true);
            if(darkroom==null)throw new Exception("House darkroom visual binding required");
            foreach(var model in darkroom.models)
            {
                string asset=model.root.GetComponent<DarkroomVisualIdentity>().assetName;
                var rest=darkroom.clips.FirstOrDefault(c=>c!=null && c.name=="AN_"+asset+"_RestAll");
                if(rest==null)continue;
                var motions=darkroom.clips.Where(c=>c!=null && c!=rest && c.name.StartsWith("AN_"+asset+"_",StringComparison.Ordinal)).ToArray();
                // Some delivered clip names use only Cxx instead of the full asset name.
                if(motions.Length==0)motions=darkroom.clips.Where(c=>c!=null && c!=rest && c.name.StartsWith("AN_"+asset.Split('_')[0]+"_",StringComparison.Ordinal)).ToArray();
                motions=motions.Where(c=>!c.name.EndsWith("_RestAll",StringComparison.Ordinal)).ToArray();
                if(asset=="C06_NegativeClip")motions=motions.Where(c=>c.name=="AN_C06_Open" || c.name=="AN_C06_Closed").ToArray();
                if(asset=="C06_NegativeStrip")motions=motions.Where(c=>c.name!="AN_C06_Open" && c.name!="AN_C06_Closed").ToArray();
                foreach(var motion in motions)WithClone(model.root,go=>{
                    rest.SamplePropAnimation(go,0);var baseline=Pose(go);motion.SamplePropAnimation(go,motion.length*.65f);
                    if(Same(baseline,Pose(go)))return; // Visibility-off/rest-equivalent channels are not a motion assertion.
                    Check(go,()=>rest.SamplePropAnimation(go,0),()=>motion.SamplePropAnimation(go,motion.length*.65f),"C "+motion.name);count++;
                });
            }
            if(count<=6)throw new Exception("No darkroom motion exercised");
            return "PROP_SAMPLING_PASS checks="+count+" platform="+Application.platform+" editor="+Application.isEditor;
        }
    }
}
