using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Temporary live Game-view staging. No scene, task, reference or save is persisted.
    // Always End before another test; domain reload automatically calls End first.
    public static class ParkInstrumentVisualAcceptance
    {
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        static ParkPhotoPrototype park;
        static ParkCompanion companion;
        static readonly Dictionary<object,Dictionary<FieldInfo,object>> state=new Dictionary<object,Dictionary<FieldInfo,object>>();
        static readonly Dictionary<Array,Array> arrays=new Dictionary<Array,Array>();
        static readonly Dictionary<Transform,Pose> poses=new Dictionary<Transform,Pose>();
        static readonly Dictionary<Renderer,RendererState> renderers=new Dictionary<Renderer,RendererState>();
        static readonly Dictionary<CharacterController,bool> motors=new Dictionary<CharacterController,bool>();
        static readonly List<UnityEngine.Object> temporary=new List<UnityEngine.Object>();
        static Camera camera;
        static float fov,aspect;
        static CursorLockMode cursorLock;
        static bool cursorVisible;
        static Texture2D oldOutlineSource;
        static RenderTexture oldOutline;
        static PhotographyMeterDisplay meter;
        static ThatGamejam.Art.LightMeterVisual.Snapshot meterSnapshot;
        static Texture2D meterContent;
        static Color32[] meterPixels;
        struct Pose {public Vector3 position,scale;public Quaternion rotation;public bool active;}
        sealed class RendererState
        {
            public bool enabled,forceOff;
            public MaterialPropertyBlock block;
            public float[] weights;
        }
        static object Get(object target,string name)=>target.GetType().GetField(name,Fields).GetValue(target);
        static void Set(object target,string name,object value)=>target.GetType().GetField(name,Fields).SetValue(target,value);
        static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Fields).Invoke(target,args);
        static void Snapshot(object target)
        {
            if(target==null || state.ContainsKey(target))return;
            var values=new Dictionary<FieldInfo,object>();state.Add(target,values);
            for(var type=target.GetType();type!=null && type.Namespace!=null && type.Namespace.StartsWith("MemoryWhitebox");type=type.BaseType)
                foreach(var field in type.GetFields(Fields|BindingFlags.DeclaredOnly))
                {
                    var value=field.GetValue(target);
                    if(value is Array && !arrays.ContainsKey((Array)value))arrays[(Array)value]=(Array)((Array)value).Clone();
                    if(!field.IsInitOnly && !field.IsLiteral)values[field]=value;
                    // Snapshot the mutable input gates too; restoring their owner reference is insufficient.
                    if(value!=null && !(value is UnityEngine.Object) && !field.FieldType.IsValueType && field.FieldType.Namespace=="MemoryWhitebox")Snapshot(value);
                }
        }
        public static string Begin()
        {
            if(park!=null)throw new InvalidOperationException("Visual acceptance already active; call End first");
            var candidate=UnityEngine.Object.FindObjectOfType<ParkPhotoPrototype>();
            if(!Application.isPlaying || candidate==null || candidate.companion==null)throw new InvalidOperationException("Existing Park Play required");
            park=candidate;companion=park.companion;camera=park.view;
            try
            {
                foreach(var root in new[]{park.transform,companion.transform})
                {
                    foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))Snapshot(component);
                    foreach(var t in root.GetComponentsInChildren<Transform>(true))
                        if(!poses.ContainsKey(t))poses[t]=new Pose{position=t.localPosition,rotation=t.localRotation,scale=t.localScale,active=t.gameObject.activeSelf};
                    foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if(renderers.ContainsKey(r))continue;
                        var value=new RendererState{enabled=r.enabled,forceOff=r.forceRenderingOff,block=new MaterialPropertyBlock()};r.GetPropertyBlock(value.block);
                        var skin=r as SkinnedMeshRenderer;
                        if(skin!=null && skin.sharedMesh!=null){value.weights=new float[skin.sharedMesh.blendShapeCount];for(int i=0;i<value.weights.Length;i++)value.weights[i]=skin.GetBlendShapeWeight(i);}
                        renderers[r]=value;
                    }
                    foreach(var motor in root.GetComponentsInChildren<CharacterController>(true))motors[motor]=motor.enabled;
                }
                Snapshot(park);Snapshot(companion);Snapshot(park.focusEffect);Snapshot(park.InputContext);
                fov=camera.fieldOfView;aspect=camera.aspect;cursorLock=Cursor.lockState;cursorVisible=Cursor.visible;
                oldOutline=(RenderTexture)Get(park,"referenceOutline");oldOutlineSource=(Texture2D)Get(park,"outlineSource");
                // Detach the live cache before generating a temporary silhouette, preserving it for End.
                Set(park,"referenceOutline",null);Set(park,"outlineSource",null);
                meter=(PhotographyMeterDisplay)Get(park,"instrumentMeter");
                if(meter!=null){meterSnapshot=meter.visual.Capture();meterContent=(Texture2D)Get(meter,"content");if(meterContent!=null)meterPixels=meterContent.GetPixels32();}
                AssemblyReloadEvents.beforeAssemblyReload+=RestoreBeforeReload;
                EditorApplication.playModeStateChanged+=OnPlayState;
                Freeze();return "Visual acceptance begun; main/actor frozen, hands active; call Show then capture real Game-view frames, finally End";
            }
            catch{End();throw;}
        }
        static void Freeze()
        {
            Set(park,"applicationPaused",true);Set(park,"focused",true);Set(park,"inputGate",false);Set(park,"changingScene",false);Set(park,"<CurrentPauseReason>k__BackingField",ParkPhotoPrototype.PauseReason.None);
            companion.Paused=true;companion.TravelPaused=true;
            var hands=camera.GetComponent<FirstPersonHands>();
            if(hands!=null){Set(hands,"handFocused",true);Set(hands,"handPaused",false);}
        }
        public static string Show(string mode)
        {
            if(park==null)throw new InvalidOperationException("Call Begin first");
            var target=(ParkPhotoPrototype.PhotographyMode)Enum.Parse(typeof(ParkPhotoPrototype.PhotographyMode),mode,true);
            if(target!=ParkPhotoPrototype.PhotographyMode.Body && target!=ParkPhotoPrototype.PhotographyMode.Aperture && target!=ParkPhotoPrototype.PhotographyMode.Shutter && target!=ParkPhotoPrototype.PhotographyMode.Meter && target!=ParkPhotoPrototype.PhotographyMode.Viewfinder)
                throw new ArgumentException("Use Body, Aperture, Shutter, Meter or Viewfinder");
            // Clone on every staging operation; no original roll/checkpoint is ever modified.
            var original=(ParkFilmRoll)state[park][typeof(ParkPhotoPrototype).GetField("roll",Fields)];
            var roll=JsonUtility.FromJson<ParkFilmRoll>(JsonUtility.ToJson(original));
            roll.cameraLoading=ParkCameraLoading.AtStep(7,400);Set(park,"roll",roll);Set(park,"loadingStep",7);
            Set(park,"apertureIndex",3);Set(park,"speedIndex",7);Set(park,"meterIsoIndex",2);Set(park,"dragPart",null);
            Set(park,"guidedDeparturePending",false);
            Call(park,"SetPhotographyMode",target);Freeze();
            var q=(Quaternion)typeof(ParkPhotoPrototype).GetMethod("InstrumentPresentationRotation",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{target});
            Set(park,"presentationStart",q);Set(park,"presentationGoal",q);Set(park,"presentationElapsed",.2f);
            if(target==ParkPhotoPrototype.PhotographyMode.Meter)Set(park,"meterHeld",1f);
            if(target==ParkPhotoPrototype.PhotographyMode.Viewfinder)StageSilhouette(roll);
            else {camera.fieldOfView=PlayerControlSettings.Current.parkWalkingFov;Set(park,"outlineAlpha",0f);}
            Call(park,"SyncInstruments");Call(park,"SyncCameraPreview");
            var hands=camera.GetComponent<FirstPersonHands>();if(hands!=null)hands.Advance(1f);
            Physics.SyncTransforms();
            return "Showing "+target+" at ISO400 f/8 1/500; main and actor frozen; capture after real frames; End restores original state";
        }
        static void StageSilhouette(ParkFilmRoll roll)
        {
            var node=(ParkPhotoWaypoint)Get(park,"bridgeNode");
            if(node==null || node.referenceView==null || node.referencePhoto==null)throw new InvalidOperationException("Pending task needs an authored view and reference photo");
            var task=roll.Task(roll.NextTask);
            if(task==null){task=new BridgePhotoCheckpoint();if(roll.NextTask=="entrance")roll.entrance=task;else if(roll.NextTask=="meadow")roll.meadow=task;else roll.bridge=task;}
            task.hasReference=true;task.aligned=false;task.captured=false;task.handJoined=false;
            task.position=node.referenceView.position;task.euler=node.referenceView.eulerAngles;task.fov=48;
            foreach(var motor in motors.Keys)if(motor!=null)motor.enabled=false;
            park.transform.position=node.referenceView.position-Vector3.up*PlayerControlSettings.Current.eyeHeight;
            camera.transform.SetPositionAndRotation(node.referenceView.position,node.referenceView.rotation);
            companion.photoHoldNode=node;companion.ResumeAtPhotoNode(node);companion.Paused=true;
            foreach(var pair in motors)if(pair.Key!=null)pair.Key.enabled=pair.Value;
            Set(park,"guidedDocked",true);Set(park,"focusDistance",18f);camera.fieldOfView=48;
            task.subjectPoint=companion.chest.position;Set(park,"outlineAlpha",.35f);
            Set(park,"bridgeReference",node.referencePhoto);
            var generated=(RenderTexture)Get(park,"referenceOutline");if(generated!=null){generated.Release();UnityEngine.Object.DestroyImmediate(generated);}
            Set(park,"referenceOutline",null);Set(park,"outlineSource",null);
            var oldMaterial=Get(park,"subjectMaskMaterial");Call(park,"PrepareReferenceOutline");
            var newMaterial=Get(park,"subjectMaskMaterial");if(oldMaterial==null && newMaterial is UnityEngine.Object)temporary.Add((UnityEngine.Object)newMaterial);
        }
        static void RestoreBeforeReload(){End();}
        static void OnPlayState(PlayModeStateChange change){if(change==PlayModeStateChange.ExitingPlayMode)End();}
        public static string End()
        {
            AssemblyReloadEvents.beforeAssemblyReload-=RestoreBeforeReload;EditorApplication.playModeStateChanged-=OnPlayState;
            if(park==null){Clear();return "Visual acceptance inactive";}
            try
            {
                var generated=(RenderTexture)Get(park,"referenceOutline");
                if(generated!=null && generated!=oldOutline){generated.Release();UnityEngine.Object.DestroyImmediate(generated);}
                foreach(var motor in motors.Keys)if(motor!=null)motor.enabled=false;
                foreach(var entry in state)
                {
                    if(entry.Key is UnityEngine.Object && (UnityEngine.Object)entry.Key==null)continue;
                    foreach(var field in entry.Value)field.Key.SetValue(entry.Key,field.Value);
                }
                foreach(var pair in arrays)Array.Copy(pair.Value,pair.Key,pair.Value.Length);
                if(meterContent!=null && meterPixels!=null){meterContent.SetPixels32(meterPixels);meterContent.Apply();}
                if(meter!=null && meterSnapshot!=null)meter.visual.Restore(meterSnapshot);
                foreach(var pair in poses)if(pair.Key!=null)
                {pair.Key.localPosition=pair.Value.position;pair.Key.localRotation=pair.Value.rotation;pair.Key.localScale=pair.Value.scale;pair.Key.gameObject.SetActive(pair.Value.active);}
                foreach(var pair in renderers)if(pair.Key!=null)
                {
                    pair.Key.enabled=pair.Value.enabled;pair.Key.forceRenderingOff=pair.Value.forceOff;pair.Key.SetPropertyBlock(pair.Value.block);
                    var skin=pair.Key as SkinnedMeshRenderer;if(skin!=null && pair.Value.weights!=null)for(int i=0;i<pair.Value.weights.Length;i++)skin.SetBlendShapeWeight(i,pair.Value.weights[i]);
                }
                foreach(var pair in motors)if(pair.Key!=null)pair.Key.enabled=pair.Value;
                camera.fieldOfView=fov;camera.aspect=aspect;Cursor.lockState=cursorLock;Cursor.visible=cursorVisible;
                Physics.SyncTransforms();
                foreach(var value in temporary)if(value!=null)UnityEngine.Object.DestroyImmediate(value);
                return "Visual acceptance ended; original runtime fields, transforms, controllers, meter and outline restored; no saves changed";
            }
            finally{Clear();}
        }
        static void Clear(){park=null;companion=null;camera=null;meter=null;meterSnapshot=null;meterContent=null;meterPixels=null;oldOutline=null;oldOutlineSource=null;state.Clear();arrays.Clear();poses.Clear();renderers.Clear();motors.Clear();temporary.Clear();}
    }
}
