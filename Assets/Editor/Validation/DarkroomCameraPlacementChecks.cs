using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MemoryWhitebox.Editor
{
    // Run in an isolated Editor play save after shortcut 2 has finished loading.
    public static class DarkroomCameraPlacementChecks
    {
        public static string Status { get; private set; } = "Not run";
        static readonly List<string> results = new List<string>();
        public static void Run(WBMain host)
        {
            if(Status=="Running")throw new InvalidOperationException("Already running");
            results.Clear();Status="Running";host.StartCoroutine(Observe(Sequence(host)));
        }
        static IEnumerator Observe(IEnumerator sequence)
        {
            while(true)
            {
                object next;
                try
                {
                    if(!sequence.MoveNext()){Status="PASS";break;}
                    next=sequence.Current;
                }
                catch(Exception error){Status="FAIL: "+error.Message;Debug.LogException(error);break;}
                yield return next;
            }
            Directory.CreateDirectory("output/QA");
            File.WriteAllLines("output/QA/darkroom-camera-placement-checks.txt",results);
            Debug.Log("DARKROOM_CAMERA_CHECKS "+Status);
        }
        static IEnumerator Sequence(WBMain host)
        {
            var world=host.DarkroomWorld;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            void Check(bool valid,string label)
            {
                results.Add((valid?"PASS ":"FAIL ")+label);
                if(!valid)throw new Exception(label);
            }
            Ray At(string id)
            {
                Physics.SyncTransforms();var obj=world.Nodes[id];var col=obj.GetComponent<Collider>();
                return new Ray((col?col.bounds.center:obj.transform.position)+Vector3.up*.9f,Vector3.down);
            }
            void Click(string id){world.Press(id,At(id),0);world.Release(id,At(id));}
            void Drag(string id,string target)
            {
                world.Press(id,At(id),0);
                if(id=="tank" && target=="film-drain-basin")
                {
                    // The direct diagonal crosses the dark bag. Carry beside it,
                    // using the same collision sweep as normal pointer movement.
                    var drag=(ObjectDragSession)world.GetType().GetField("objectDrag",flags).GetValue(world);
                    var item=world.Nodes[id].transform;var basin=world.Nodes[target].transform;
                    drag.MoveTo(new Vector3(basin.position.x,item.position.y+.25f,item.position.z),basin);
                    Check(!drag.Blocked,"Carry tank around dark bag");
                }
                world.Move(At(target));world.Release(target,At(target));
            }
            bool Flag(string name)=>(bool)world.GetType().GetField(name,flags).GetValue(world);
            var settle=new WaitForSeconds(.7f);
            Check(world.Photo.Phase==PhotoChapter.FilmPhase.Exposed,"Fresh exposed roll retains full rewind workflow");
            Check(host.DirectDarkroomActive && host.InputContext.Ready,"Shortcut enters ready darkroom mode");
            var target=world.Nodes["rewind"].transform;
            var view=host.Room.Camera;var screen=view.WorldToViewportPoint(target.position);
            Check(screen.z>0 && screen.x>0 && screen.x<1 && screen.y>0 && screen.y<1,"Rewind target visible from shortcut view");
            Check(world.Pick(new Ray(view.transform.position,target.position-view.transform.position))=="rewind","Rewind target reachable and unobstructed");
            world.RefreshDarkroomHighlight();
            var cue=target.GetComponent<TaskOutlineCue>();
            Check(cue!=null,"Next-step outline requested on rewind knob");
            var outline=typeof(TaskOutlineCue).GetField("outline",flags).GetValue(cue);
            Check((Camera)outline.GetType().GetField("camera",flags).GetValue(outline)==view,"Outline attached to player camera");
            var ink=(Material)outline.GetType().GetField("material",flags).GetValue(outline);
            Check(ink.GetColor("_InkColor")==new Color(1,.65f,.12f,1),"Next-step ink is orange");
            world.Press("rewind",At("rewind"),0);world.Scroll("rewind",8,At("rewind"));world.Release("rewind",At("rewind"));
            Click("back");Check(world.Photo.Phase==PhotoChapter.FilmPhase.Rewound && world.RewoundBackOpen,"Rewind and open moved camera back");
            world.Press("exposed-film",At("exposed-film"),0);
            Check(world.CarryingExposedFilm,"Extract film from moved camera");yield return settle;
            world.Move(At("bag"));world.Release("bag",At("bag"));
            Check(world.Photo.Phase==PhotoChapter.FilmPhase.DarkBag,"Carry film directly to adjacent dark bag");yield return settle;
            world.Press("bag",At("bag"),0);world.Scroll("bag",10,At("bag"));world.Release("bag",At("bag"));Click("tank-lid");
            Check(world.Photo.Phase==PhotoChapter.FilmPhase.Tank && Flag("tankSealed"),"Load reel and seal tank");
            for(int chemistry=0;chemistry<3;chemistry++)
            {
                Drag("chemical-"+chemistry,"tank");Check(world.Photo.Phase==PhotoChapter.FilmPhase.Processing,"Pour chemical "+chemistry);yield return settle;
                var ray=At("tank");world.Press("tank",ray,0);world.Move(new Ray(ray.origin+Vector3.right*.2f,ray.direction));world.Release("tank",ray);
                Check(world.Photo.Agitated,"Agitate chemical "+chemistry);world.TickDirectDarkroom(100);yield return settle;
                Check(Flag("needsDrain"),"Drain requested "+chemistry);Drag("tank","film-drain-basin");
                Check(!Flag("needsDrain"),"Drain chemical "+chemistry);yield return settle;
            }
            Click("water");world.TickDirectDarkroom(100);
            Check(world.CurrentPrintStage==ParkPrintStage.NegativeDrying,"Rinse negative");
            Click("tank-lid");Drag("print-negative","print-negative-clip");yield return settle;world.TickDirectDarkroom(100);
            Check(world.CurrentPrintStage==ParkPrintStage.ContactReady,"Hang and dry negative");
            Drag("print-paper","print-frame");yield return settle;
            Drag("print-negative","print-frame");yield return settle;Click("print-frame-lid");yield return settle;
            world.Press("print-exposure",At("print-exposure"),0);world.TickDirectDarkroom(100);world.Release("print-exposure",At("print-exposure"));
            Check(world.CurrentPrintStage==ParkPrintStage.PaperDeveloper,"Expose contact paper");Click("print-frame-lid");yield return settle;
            foreach(var tray in new[]{"print-developer","print-stop","print-fixer"})
            {
                Drag("print-paper",tray);Check(Flag("printProcessing"),"Paper enters "+tray);yield return settle;world.TickDirectDarkroom(100);
            }
            Drag("print-paper","print-wash");yield return settle;Click("print-tap");world.TickDirectDarkroom(100);
            Drag("print-paper","print-photo-clip");yield return settle;world.TickDirectDarkroom(100);
            Check(world.DevelopedRollId!=null,"Finished print available");Check(world.SaveProgress(),"Completed workflow saved");
        }
    }
}
