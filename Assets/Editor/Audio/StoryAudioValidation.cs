using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.EditorTools
{
    public static class StoryAudioValidation
    {
        const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        static void Check(bool ok,string label)
        {if(!ok)throw new InvalidOperationException("STORY_AUDIO: "+label);}
        static void Set(object target,string name,object value)
        {target.GetType().GetField(name,Hidden).SetValue(target,value);}
        static object Call(object target,string name,params object[] args)
        {return target.GetType().GetMethod(name,Hidden).Invoke(target,args);}

        public static string Run()
        {
            var bank=Resources.Load<TapeSfxBank>("StorySfxBank");
            Check(bank!=null && bank.cues.Length>=43 && bank.cues.Select(c=>c.id).Distinct().Count()==bank.cues.Length,"unique cue IDs including restoration additions");
            foreach(var cue in bank.cues)
            {
                Check(cue.clip!=null && cue.duration>.02f && cue.start+cue.duration<=cue.clip.length+.002f,"clip bounds "+cue.id);
                Check(cue.clip.LoadAudioData(),"load PCM "+cue.id);
                var pcm=new float[cue.clip.samples*cue.clip.channels];
                Check(cue.clip.GetData(pcm,0),"read PCM "+cue.id);
                float peak=0;
                foreach(float value in pcm)
                {
                    Check(!float.IsNaN(value) && !float.IsInfinity(value),"finite PCM");
                    peak=Mathf.Max(peak,Mathf.Abs(value));
                }
                Check(peak>.001f && peak<.58f,"audible, unclipped slice "+cue.id+" peak="+peak);
            }
            var root=new GameObject("Isolated story audio checks"){hideFlags=HideFlags.HideAndDontSave};
            var cues=new List<int>();
            Action<int,bool> collect=(id,loop)=>cues.Add(id);
            StorySfx.CueRequested+=collect;
            try
            {
                var host=root.AddComponent<WBMain>();host.enabled=false;
                host.safeInspection=root.AddComponent<SafeInspection>();host.safeInspection.enabled=false;
                var cameraObject=new GameObject("Test camera");cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;host.playerCamera=camera;
                Set(host,"<Room>k__BackingField",new RoomWorld{Camera=camera});
                CheckTape(root,host,camera);
                CheckPlayer(root,cues);
                CheckEnding(root,host,camera,cues);
                return "PASS "+bank.cues.Length+" bank clips and PCM; 004 actual first approach and manual lid; 013 live transport status both directions and ends; 003 actual displacement threshold, 103 hands; 050 door, 150 outward threshold and restoration silence.";
            }
            finally{StorySfx.CueRequested-=collect;UnityEngine.Object.DestroyImmediate(root);}
        }

        static void CheckTape(GameObject root,WBMain host,Camera camera)
        {
            var deck=root.AddComponent<HouseRadioDeck>();deck.enabled=false;
            deck.house=host;deck.view=camera;
            var visual=root.AddComponent<TapeWorkbenchVisuals>();visual.enabled=false;
            visual.storage=root.AddComponent<WorkbenchAssetView>();
            deck.workbenchVisuals=visual;
            var audio=root.AddComponent<TapeSfxPlayer>();audio.bank=Resources.Load<TapeSfxBank>("TapeSfxBank");
            Set(deck,"sfx",audio);
            Set(deck,"activeCloseView",root.AddComponent<SceneViewAnchor>());
            Set(deck,"radioFocused",true);
            Set(deck,"radioStartRotation",Quaternion.identity);
            Set(deck,"radioCameraMoving",true);
            Set(deck,"openStorageOnArrival",true);
            Call(deck,"TickRadioCamera",3f);
            var next=typeof(TapeSfxPlayer).GetField("next",Hidden);
            Check(visual.LidOpen && (int)next.GetValue(audio)==1,"first approach opens and plays one 004");
            Check(audio.GetComponentsInChildren<AudioSource>().Any(a=>a.clip==audio.bank.Find(4).clip),"004 routed to configured clip");
            Call(deck,"TickRadioCamera",3f);
            Call(deck,"ToggleStorageLidWithSound");
            Check((int)next.GetValue(audio)==1,"arrival or busy lid cannot repeat 004");
            Set(visual,"lid",1f);Call(deck,"ToggleStorageLidWithSound");
            Check(!visual.LidOpen && (int)next.GetValue(audio)==2,"manual close plays one 004");
            var transport=root.AddComponent<TapeSampleTransport>();transport.enabled=false;
            Set(transport,"source",new float[24]);Set(transport,"configured",true);Set(transport,"a",12);
            deck.transport=transport;deck.sourceSocket=root.transform;deck.slotHit=root.AddComponent<BoxCollider>();
            Set(deck,"<Chapter>k__BackingField",new RadioTapeChapter{SourceLoaded=true});Set(deck,"focused",true);
            var voice=((Array)typeof(TapeSfxPlayer).GetField("loops",Hidden).GetValue(audio)).GetValue(0);
            var wanted=voice.GetType().GetField("wanted");
            var source=(AudioSource)voice.GetType().GetField("source").GetValue(voice);
            foreach(int direction in new[]{-1,1})
            {
                Set(transport,"a",12);Set(transport,"sourceSeek",direction);
                Call(deck,"TickSfx");
                Check((bool)wanted.GetValue(voice) && source.clip==audio.bank.Find(13).clip,"A winding selects 013 "+direction);
                Call(transport,"OnAudioFilterRead",new float[4],1);Call(deck,"TickSfx");
                Check(!transport.SourceRunning && !(bool)wanted.GetValue(voice),"A winding stops at physical end "+direction);
            }
            Set(transport,"a",0);Set(transport,"playA",true);Call(deck,"TickSfx");
            Check((bool)wanted.GetValue(voice) && source.clip==audio.bank.Find(12).clip,"A playback selects 012");
            Set(transport,"configured",false);
        }

        static void CheckPlayer(GameObject root,List<int> cues)
        {
            var type=typeof(RoomWorld).Assembly.GetType("MemoryWhitebox.PlayerFootsteps");
            var steps=Activator.CreateInstance(type);
            var advance=type.GetMethod("Advance");
            advance.Invoke(steps,new object[]{Vector3.zero,Vector3.up*2,Vector3.zero});
            advance.Invoke(steps,new object[]{Vector3.zero,Vector3.right*.4f,Vector3.zero});
            Check(cues.Count==0,"blocked, vertical or short travel is silent");
            advance.Invoke(steps,new object[]{Vector3.zero,Vector3.right*.24f,Vector3.zero});
            Check(cues.SequenceEqual(new[]{3}),"actual displacement crosses one stride");
            type.GetMethod("Reset").Invoke(steps,null);
            advance.Invoke(steps,new object[]{Vector3.zero,Vector3.right*.6f,Vector3.zero});
            Check(cues.Count==1,"reposition resets stride remainder");
            var park=root.AddComponent<ParkPhotoPrototype>();
            var hands=root.GetComponentInChildren<Camera>().gameObject.AddComponent<FirstPersonHands>();hands.enabled=false;
            hands.park=park;
            Call(hands,"UpdateClothingSound");
            Set(park,"photoMode",ParkPhotoPrototype.PhotographyMode.Body);
            Call(hands,"UpdateClothingSound");Call(hands,"UpdateClothingSound");
            Check(cues.SequenceEqual(new[]{3,103}),"raising changes hands once, stable pose does not repeat");
        }

        static void CheckEnding(GameObject root,WBMain host,Camera camera,List<int> cues)
        {
            cues.Clear();
            var binding=root.AddComponent<NarrativeSceneBinding>();
            var door=GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(root.transform,false);door.transform.localScale=new Vector3(1,2,.1f);
            binding.entranceDoor=door.transform;binding.threshold=root.transform;
            binding.doorwayHalfWidth=.5f;
            var ending=new EndingChapter();
            var world=new EndingWorld3D(host,ending);
            try
            {
                camera.transform.position=new Vector3(0,0,1);
                var ray=new Ray(camera.transform.position,Vector3.back);
                world.StepHouse(.01f,ray,false);
                Check(cues.Count==0,"door initial pose is silent");
                world.StepHouse(1,ray,true);world.StepHouse(.1f,ray,true);
                Check(cues.SequenceEqual(new[]{50}) && ending.DoorOpen,"valid handle opens once");
                camera.transform.position=new Vector3(1,0,-1);
                world.StepHouse(.01f,ray,false);
                Check(cues.Count==1,"outside doorway does not trigger crossing");
                camera.transform.position=new Vector3(0,0,1);world.StepHouse(.01f,ray,false);
                camera.transform.position=new Vector3(0,0,-1);world.StepHouse(.01f,ray,false);
                world.StepHouse(.01f,ray,false);
                Check(cues.SequenceEqual(new[]{50,150}) && ending.Completed,"outward threshold plays once");
                world.RestoreDoorPose();world.StepHouse(.01f,ray,false);
                Check(cues.Count==2,"restored door is silent");
            }
            finally{world.Dispose();}
        }
        public static string CheckSummerResponse()
        {
            string qa=Path.GetFullPath(Path.Combine(Application.dataPath,"..","output","QA"));
            string directory=Path.Combine(qa,"story-sfx-summer-"+Guid.NewGuid().ToString("N"));
            var root=new GameObject("Isolated summer response cues"){hideFlags=HideFlags.HideAndDontSave};
            root.SetActive(false);root.transform.position=new Vector3(1000,20,1000);
            var image=new Texture2D(2,2,TextureFormat.RGB24,false);
            var cues=new List<int>();
            Action<int,bool> collect=(id,loop)=>cues.Add(id);
            StorySfx.CueRequested+=collect;
            try
            {
                using(ParkFilmStore.BeginValidationScope(directory))
                {
                    var park=root.AddComponent<ParkPhotoPrototype>();
                    park.view=root.AddComponent<Camera>();
                    var actor=new GameObject("Companion");actor.transform.SetParent(root.transform,false);
                    actor.transform.localPosition=Vector3.forward*3;
                    park.companion=actor.AddComponent<ParkCompanion>();park.companion.chest=actor.transform;
                    var node=root.AddComponent<ParkPhotoWaypoint>();
                    park.companion.route=new[]{node};Set(park.companion,"atNode",true);
                    Set(park,"bridgeNode",node);park.focusDistance=3;
                    var roll=ParkFilmStore.SaveCameraLoading(null,ParkCameraLoading.AtStep(7,400));
                    byte[] png=image.EncodeToPNG();
                    foreach(string task in new[]{"entrance","bridge","meadow"})
                    {
                        float ev=ParkCameraResponse.ExposureError(park.Aperture,park.ShutterSeconds,roll.FilmIso,0);
                        var reference=new BridgePhotoCheckpoint{hasReference=true,position=park.view.transform.position,
                            subjectPoint=actor.transform.position,ev100=ev};
                        roll=ParkFilmStore.SaveTask(roll.id,task,reference,png);
                        Set(park,"roll",roll);Set(park,"<Aiming>k__BackingField",false);cues.Clear();
                        Call(park,"TickBridge",1f);
                        Check(cues.Count==0 && !ParkFilmStore.Load(roll.id).Task(task).aligned,"unaligned summer task silent "+task);
                        Set(park,"<Aiming>k__BackingField",true);Call(park,"TickBridge",1f);
                        roll=ParkFilmStore.Load(roll.id);
                        Check(roll.Task(task).aligned && cues.SequenceEqual(task=="entrance"?Array.Empty<int>():new[]{36}),"summer alignment cue "+task);
                        Set(park,"roll",roll);cues.Clear();Call(park,"TickBridge",1f);
                        Check(cues.Count==0,"restored summer alignment silent "+task);
                        if(task!="meadow")
                        {
                            var record=new ParkPhotoRecord{id=Guid.NewGuid().ToString("N"),taskId=task,iso=400,accepted=true,capturedUtc=DateTime.UtcNow.ToString("o")};
                            roll=ParkFilmStore.SaveExposure(roll,record,png);
                        }
                    }
                }
                return "PASS 036 summer bridge and meadow after committed alignment only; entrance and restored state silent.";
            }
            finally
            {
                StorySfx.CueRequested-=collect;
                UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(image);
                if(Path.GetDirectoryName(Path.GetFullPath(directory))==qa && Directory.Exists(directory))Directory.Delete(directory,true);
            }
        }
    }
}
