using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace MemoryWhitebox.Editor
{
    public static class ContinuousDarkroomChecks
    {
        public static void Run(WBMain host)
        {
            var checks=new List<string>();var world=host.DarkroomWorld;
            void Check(bool ok,string label){if(!ok)throw new Exception("DARKROOM: "+label+"; "+world.Help);checks.Add("PASS "+label);File.WriteAllLines("QA/continuous-darkroom-checks.txt",checks);}
            Ray At(string id){Physics.SyncTransforms();var obj=world.Nodes[id];var col=obj.GetComponent<Collider>();var p=col!=null?col.bounds.center:obj.transform.position;return new Ray(p+Vector3.up*.9f,Vector3.down);}
            void Press(string id){world.Press(id,At(id),0);}
            void Click(string id){Press(id);world.Release(id,At(id));}
            void Drag(string item,string target){Press(item);world.Move(At(target));world.Release(target,At(target));}
            void CarryFilmThroughSummerDoor(){
                var summer=UnityEngine.Object.FindObjectOfType<SummerPhotoRoom>();
                if(summer==null){Drag("exposed-film","bag");return;}
                Press("exposed-film");var drag=Field<ObjectDragSession>("objectDrag");
                Check(world.CarryingExposedFilm && drag.Active,"Hold the rewound cartridge");
                var cam=host.Room.Camera.transform;var previous=cam.position;
                var portal=summer.passageBarrier.transform.position;
                void Carry(Vector3 point,string leg){Physics.SyncTransforms();drag.MoveTo(point);Check(!drag.Blocked,"Carry around bedroom "+leg+" from "+drag.Item.position+" to "+point);}
                try{
                    var from=drag.Item.position;float height=1.55f;
                    Carry(new Vector3(from.x,height,from.z),"lift out of camera");
                    Carry(new Vector3(portal.x,height,from.z),"to central aisle");
                    Carry(new Vector3(portal.x,height,portal.z-.6f),"toward curtain");
                    cam.position=new Vector3(portal.x,1.65f,portal.z-.6f);
                    summer.TickCurtain(host,1f);Physics.SyncTransforms();
                    Check(!summer.passageBarrier.gameObject.activeSelf,"Carried film opens the actual summer curtain");
                    Carry(new Vector3(portal.x,height,portal.z+.64f),"through open doorway");
                    var ray=At("bag");world.Move(ray);
                    world.Release("bag",ray);
                }
                finally{cam.position=previous;}
            }
            T Field<T>(string name)=>(T)world.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world);
            Check(world.DebugPrepareDarkroom(out var message),"Debug roll created without entering park");
            if(host.Curtain!=null&&!host.Curtain.IsOpen){host.Curtain.Toggle();host.Curtain.Advance(1);}
            host.PrepareContinuousDarkroom();world.SetDirectDarkroom(true);host.SyncInputContext();host.InputContext.ContinueGesture();
            Press("rewind");world.Scroll("rewind",8,At("rewind"));world.Release("rewind",At("rewind"));var openingRay=At("rewind");world.Press("rewind",openingRay,0);world.Move(new Ray(openingRay.origin+world.View.transform.up*.04f,openingRay.direction));world.Release("rewind",openingRay);
            Check(world.Photo.Phase==PhotoChapter.FilmPhase.Rewound,"Rewind exposed roll");
            CarryFilmThroughSummerDoor();Check(world.Photo.Phase==PhotoChapter.FilmPhase.DarkBag,"Exposed roll enters dark bag");
            Press("bag");world.Scroll("bag",10,At("bag"));world.Release("bag",At("bag"));Click("tank-lid");
            Check(world.Photo.Phase==PhotoChapter.FilmPhase.Tank,"Load and seal developing tank");
            for(int chemistry=0;chemistry<3;chemistry++)
            {
                Drag("chemical-"+chemistry,"tank");Check(world.Photo.Phase==PhotoChapter.FilmPhase.Processing,"Pour chemical "+chemistry);
                var ray=At("tank");world.Press("tank",ray,0);world.Move(new Ray(ray.origin+Vector3.right*.2f,ray.direction));world.Release("tank",ray);
                Check(world.Photo.Agitated,"Agitate chemical "+chemistry);world.TickDirectDarkroom(4);
                Check(Field<bool>("needsDrain"),"Drain required "+chemistry);Check(world.SaveProgress(),"Save at drain "+chemistry);
                Check((bool)world.GetType().GetMethod("LoadParkFilm",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null),"Reload saved film "+chemistry);
                world.SetDirectDarkroom(false);world.SetDirectDarkroom(true);host.SyncInputContext();host.InputContext.ContinueGesture();
                Check(Field<bool>("needsDrain"),"Leaving operation retains drain "+chemistry);
                Drag("tank","film-drain-basin");Check(!Field<bool>("needsDrain"),"Drain into basin "+chemistry);
            }
            Click("water");world.TickDirectDarkroom(2);Check(Field<ParkPrintStage>("printStage")==ParkPrintStage.NegativeDrying,"Rinse starts negative drying workflow");
            Click("tank-lid");Drag("print-negative","print-negative-clip");world.TickDirectDarkroom(100);
            Check(Field<ParkPrintStage>("printStage")==ParkPrintStage.ContactReady,"Negative dries on clip");
            Drag("print-paper","print-frame");Drag("print-negative","print-frame");Click("print-frame-lid");Press("print-exposure");world.TickDirectDarkroom(100);world.Release("print-exposure",At("print-exposure"));
            Check(Field<ParkPrintStage>("printStage")==ParkPrintStage.PaperDeveloper,"Contact exposure completes");Click("print-frame-lid");
            foreach(string target in new[]{"print-developer","print-stop","print-fixer"}){Drag("print-paper",target);Check(Field<bool>("printProcessing"),"Paper enters "+target);world.TickDirectDarkroom(100);}
            Drag("print-paper","print-wash");Click("print-tap");world.TickDirectDarkroom(100);Drag("print-paper","print-photo-clip");world.TickDirectDarkroom(100);
            Check((bool)world.GetType().GetProperty("ParkPrintsReady",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world),"Washed and dried photographs become viewable");Check(world.SaveProgress(),"Finished prints saved");
            Check((bool)world.GetType().GetMethod("LoadParkFilm",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null),"Reload finished film");
            Check((bool)world.GetType().GetProperty("ParkPrintsReady",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(world),"Finished photographs remain viewable after reload");
            world.SetDirectDarkroom(false);host.SyncInputContext();host.InputContext.ContinueGesture();Debug.Log("CONTINUOUS_DARKROOM_PASS "+checks.Count);
        }
    }
}
